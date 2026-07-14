// Tests for the fuzzy URL-target comparison: design folders link with file
// URLs ('about.html'), live Xperience sites use extensionless routes
// ('/en/about'), and domain-root links must not all collapse to "equal".
// Runs without a browser: node --test test/

import assert from 'node:assert/strict';
import { test } from 'node:test';

import { isUnresolvedVirtualUrl, sameUrlTarget } from '../src/shared/urls.ts';

test('different domain roots are different targets', () => {
  assert.equal(sameUrlTarget('https://facebook.com/', 'https://twitter.com/'), false);
  assert.equal(sameUrlTarget('https://facebook.com', 'https://twitter.com/'), false);
});

test('same-domain and relative root links match', () => {
  assert.equal(sameUrlTarget('https://example.com/', 'https://example.com'), true);
  assert.equal(sameUrlTarget('/', 'https://example.com/'), true);
  assert.equal(sameUrlTarget('index.html', '/'), true);
});

test('design .html files match extensionless live routes', () => {
  assert.equal(sameUrlTarget('about.html', '/about'), true);
  assert.equal(sameUrlTarget('about.html', '/en/about'), true);
  assert.equal(sameUrlTarget('sub/index.html', 'https://example.com/sub'), true);
  assert.equal(sameUrlTarget('about.html', '/contact'), false);
});

test('query and hash are ignored', () => {
  assert.equal(sameUrlTarget('about.html#team', '/about?utm=x'), true);
});

test('protocol-relative URLs carry their host', () => {
  assert.equal(sameUrlTarget('//cdn.example.com/logo.png', 'https://cdn.example.com/logo.png'), true);
  assert.equal(sameUrlTarget('//cdn.example.com/logo.png', 'https://other.com/logo.png'), false);
});

test('missing values match only each other', () => {
  assert.equal(sameUrlTarget(undefined, undefined), true);
  assert.equal(sameUrlTarget('/about', undefined), false);
  assert.equal(sameUrlTarget('', '/about'), false);
});

test('unresolved virtual URL detection is unchanged', () => {
  assert.equal(isUnresolvedVirtualUrl('~/media/logo.png'), true);
  assert.equal(isUnresolvedVirtualUrl('/media/logo.png'), false);
});
