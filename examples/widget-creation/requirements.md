# Article page showcase widget

## Selection

The widget allows marketers to select 1-4 article pages.

## Presentation options

- **Template:** Select between Full (art-designed, best for single article) or Minimal (optimized for multiple articles)
- **Teaser image:** Toggle to show or hide the teaser image
- **Related articles:** Toggle to show or hide related articles section

## Content retrieval

- Retrieve all linked content (related articles, images)
- Display articles in the order they were selected
- Use the correct Xperience by Kentico APIs for webpage content retrieval and caching
- Generate URLs for article links and linked image assets

## Styling

Use the existing `Site.css` styles to match the design of other components on the site.

## Error handling

1. **Missing articles:** If selected articles are missing from the database, display a message to the marketer only (not visible in live mode)
2. **Missing images:** If articles have no linked image, display a placeholder image

## Technical requirements

- Use proper caching mechanisms
- Use correct APIs for webpage content retrieval
- Generate URLs correctly for articles and assets
- Follow Xperience by Kentico best practices
