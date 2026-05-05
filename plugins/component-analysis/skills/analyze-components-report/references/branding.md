# Branding guidance for report generation

The generated HTML report should follow Kentico visual identity and remain highly scannable for large datasets.

## Canonical branding references

- [What do we look like](https://brand.kentico.com/What-do-we-look-like)
- [Logotype](https://brand.kentico.com/What-do-we-look-like/Logotype)
- [Colors](https://brand.kentico.com/What-do-we-look-like/Colors)
- [Typography](https://brand.kentico.com/What-do-we-look-like/Typography)
- [Design elements](https://brand.kentico.com/What-do-we-look-like/Design-elements)

## Practical report UI guidance

- Prioritize fast scanning: sticky top summary, compact status chips, sortable/comparable tables, and collapsible category sections.
- Support deep inspection: each category section must expose findings, checks, recommendations, evidence, and docs references.
- Keep structure resilient for large projects: avoid excessively tall card grids, and prefer table layouts with sticky headers and clear grouping.
- Preserve accessibility and contrast: all severity/risk color chips should remain readable and non-color-dependent.

## Implementation notes

- Use `report-template.html` as the base structure and map official brand colors/typography from the links above.
- If brand tokens cannot be reliably retrieved during generation, keep the template structure and output neutral fallback styles rather than inventing unofficial values.
- Do not include external CSS/JS dependencies in the generated report.
