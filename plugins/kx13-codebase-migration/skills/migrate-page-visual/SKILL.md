---
name: migrate-page-visual
description: "Ensures a migrated page visually matches the original Kentico Xperience 13 page. Uses Playwright to compare both pages and iteratively fixes styling, layout, and content differences. Use when a page migration results in visual discrepancies that need to be corrected."
argument-hint: "Name of the page, the legacy page URL, and the new page URL"
compatibility: "Requires Playwright MCP"
---

You are tasked with ensuring that the migrated page visually matches the original legacy page.

## Input Parameters

- **Page Name** - The name/path of the page being matched (e.g., 'home', 'doctors').
- **Legacy Page URL** - The URL of the page in the KX13 project (e.g., 'https://localhost:5001/en-us/home').
- **New Page URL** - The URL of the page in the XbyK project (e.g., 'http://localhost:60444/en-us/home').

## Structure of the projects

You are currently located in the root folder, which contains two subfolders:

- `KX13/` - This folder contains the Kentico Xperience 13 project files. This is the legacy/source project.
- `XbyK/` - This folder contains the Xperience by Kentico project files. This is the new project.

## Context

The page migration from KX13 to XbyK has been completed in the previous step. The result is a functional page, but it does not visually match the original. Your task is to make the new page look completely identical to the old one.

## Important Principles

1. **Dynamic content** - When migrating a page, ensure that everything that was fetched dynamically from the database will still be dynamically fetched from the database. Nothing can be statically hardcoded in the new project if it was dynamic in the legacy project.
2. **Pixel-perfect matching** - The goal is to make the pages visually identical, including layout, spacing, colors, fonts, and responsive behavior.
3. **Preserve functionality** - While fixing visual issues, ensure all functionality remains intact.

## Visual Matching Steps

1. **Ensure XbyK is running** - Start the application if not already running.

2. **Capture both pages** - Use Playwright MCP to navigate to both pages:
   - Navigate to the legacy page URL
   - Take a snapshot or screenshot for reference
   - Navigate to the new page URL
   - Take a snapshot or screenshot for comparison

3. **Identify visual differences** - Compare the two pages and note all differences:
   - Layout and structure
   - Colors and backgrounds
   - Typography (fonts, sizes, weights)
   - Spacing and margins
   - Images and media
   - Interactive elements (buttons, links, forms)

4. **Fix each difference** - For each visual discrepancy:
   - Identify the source of the difference (CSS, HTML structure, missing content)
   - Make the necessary changes in the XbyK project
   - Prefer fixing via CSS/styling over changing HTML structure
   - Implement retrieval of missing content dynamically from the database
   - Nothing can be hardcoded if it was dynamic in the legacy project

5. **Rebuild and restart** - After making changes:
   - Verify the project builds without errors
   - Restart the XbyK application

6. **Verify the fix** - Use Playwright MCP to check if the difference is resolved

7. **Iterate** - Repeat steps 3-6 until both pages are visually identical

## Output format

When done, provide the user with this exact output (without any additional text):

```
# Migration Complete
Page visual match adjustment has been successfully completed.

**Next steps:**
- Review the changes to ensure everything looks as expected.
- Continue migrating other pages by repeatedly running migrate-page-widgets and migrate-page.
```
