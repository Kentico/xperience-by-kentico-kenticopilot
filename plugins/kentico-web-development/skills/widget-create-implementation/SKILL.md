---
name: "widget-create-implementation"
description: "Creates widget code following the generated instructions, project conventions, and Xperience best practices."
argument-hint: "Path to the file containing instructions for widget creation"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with the process of creating a new widget following given instructions.

## Input Parameters

- **Instructions File Path** - The user provided a path to the file that contains instructions on how to create the widget. You must follow these during implementation.

## Steps to follow

- First, read the instructions file provided by the user.

- Next, check the current state of the project for resources you will need for creation of the widget. If you find already present widgets, follow their patterns and conventions.

- Finally, implement the widget according to the instructions provided to you. Make sure to follow best practices and conventions used in the project.

## Important rules

- **Caching** - Always use caching, unless explicitly stated otherwise.
- **Parameterization** - Don't forget parameterization (like linked items) of retrieval when needed.
- **Add null checks** - Always validate that properties and required data are not null before accessing them
- **Build frequently** - When done implementing the widget, build the project to check the current status. Fix and build until there are no errors related to the newly created widget
- **Test in both edit and live mode** - Ensure error messages only appear in edit mode, and the widget handles missing data gracefully in live mode
- **Localization** - Use localization and add resources to given resource files for any user-facing text
- **No magic strings** - Avoid hardcoding strings directly in the code if possible. Use constants or `nameof` expressions.

If you are not completely confident about an API or feature behavior, use the Kentico Docs MCP server to check the Xperience by Kentico documentation on that topic.
