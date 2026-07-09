// The in-page DOM extractor.
//
// extractPage() is serialized into the browser via page.evaluate
// (Function.prototype.toString), so it must stay fully self-contained:
// no runtime imports, no closures over module scope. Type-only imports and
// annotations are fine — Node strips them before the function is serialized.

import type { PageMeta, RawNode } from '../shared/types.ts';

/** Options serialized into the browser alongside extractPage(). */
export interface ExtractPageOptions {
  ignoreSelectors: string[];
  styleProperties: string[];
}

/** What extractPage() returns: page metadata plus the visible DOM tree. */
export interface Extraction {
  meta: PageMeta;
  tree: RawNode | null;
}

/**
 * Runs inside the page. Walks document.body and returns
 * { meta: { lang, title, url }, tree } where tree mirrors the visible DOM.
 */
export function extractPage({ ignoreSelectors, styleProperties }: ExtractPageOptions): Extraction {
  const SKIP_TAGS = new Set(['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE', 'LINK', 'META', 'IFRAME', 'SVG']);

  /** ARIA roles implied by HTML5 sectioning tags. */
  const IMPLICIT_ROLES: Record<string, string> = {
    HEADER: 'banner',
    NAV: 'navigation',
    MAIN: 'main',
    ASIDE: 'complementary',
    FOOTER: 'contentinfo',
    FORM: 'form',
    SECTION: 'region',
    ARTICLE: 'article',
  };

  /** True when the element matches, or sits inside, any --ignore selector. */
  function isIgnored(el: Element): boolean {
    return ignoreSelectors.some((sel) => {
      try {
        return el.matches(sel) || el.closest(sel) !== null;
      } catch {
        return false;
      }
    });
  }

  /** True when the element is rendered (display, visibility, aria-hidden). */
  function isVisible(el: Element, style: CSSStyleDeclaration): boolean {
    if (style.display === 'none' || style.visibility === 'hidden') return false;
    if (el.getAttribute('aria-hidden') === 'true') return false;
    return true;
  }

  /** CSS path for locating the element in a report (id-anchored where possible). */
  function cssPath(el: Element): string {
    const parts: string[] = [];
    let node: Element | null = el;
    while (node && node.nodeType === 1 && node.tagName !== 'HTML') {
      const current: Element = node;
      const tag = current.tagName.toLowerCase();
      if (current.id) {
        parts.unshift(`#${CSS.escape(current.id)}`);
        break;
      }
      let part = tag;
      const parent: Element | null = current.parentElement;
      if (parent) {
        const sameTag = Array.from(parent.children).filter((c) => c.tagName === current.tagName);
        if (sameTag.length > 1) {
          part += `:nth-of-type(${sameTag.indexOf(current) + 1})`;
        }
      }
      parts.unshift(part);
      node = parent;
    }
    return parts.join(' > ');
  }

  /** Text of the element's direct text nodes, whitespace-collapsed. */
  function ownText(el: Element): string {
    let text = '';
    for (const child of el.childNodes) {
      if (child.nodeType === 3) text += child.textContent;
    }
    return text.replace(/\s+/g, ' ').trim();
  }

  /** Recursively extract the visible subtree rooted at el; null when skipped. */
  function walk(el: Element): RawNode | null {
    if (SKIP_TAGS.has(el.tagName.toUpperCase())) return null;
    if (isIgnored(el)) return null;
    const style = getComputedStyle(el);
    if (!isVisible(el, style)) return null;

    const styles: Record<string, string> = {};
    for (const prop of styleProperties) styles[prop] = style.getPropertyValue(prop);

    const attrs: Record<string, string> = {};
    for (const name of ['href', 'src', 'srcset', 'alt', 'type', 'placeholder', 'value']) {
      const v = el.getAttribute(name); // authored value — unresolved '~/' stays visible
      if (v !== null) attrs[name] = v;
    }

    const headingMatch = el.tagName.match(/^H([1-6])$/);

    const node: RawNode = {
      tag: el.tagName.toLowerCase(),
      role: el.getAttribute('role') || IMPLICIT_ROLES[el.tagName] || null,
      id: el.id || null,
      classes: Array.from(el.classList),
      selector: cssPath(el),
      ownText: ownText(el),
      fullText: '', // assembled below in document order from visible content only
      attrs,
      headingLevel: headingMatch ? Number(headingMatch[1]) : null,
      styles,
      children: [],
    };

    const walkedByElement = new Map<Node, RawNode>();
    for (const child of el.children) {
      const c = walk(child);
      if (c) {
        node.children.push(c);
        walkedByElement.set(child, c);
      }
    }

    // Full visible text in document order: own text nodes interleaved with
    // the (already filtered) text of walked children.
    let fullText = '';
    for (const childNode of el.childNodes) {
      if (childNode.nodeType === 3) {
        fullText += childNode.textContent;
      } else if (childNode.nodeType === 1) {
        const walked = walkedByElement.get(childNode);
        if (walked) fullText += ` ${walked.fullText} `;
      }
    }
    node.fullText = fullText.replace(/\s+/g, ' ').trim();
    return node;
  }

  const tree = walk(document.body);
  return {
    meta: {
      lang: document.documentElement.getAttribute('lang') || null,
      title: document.title || null,
      url: location.href,
    },
    tree,
  };
}
