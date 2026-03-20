---
name: "widget-create-research"
description: "Prepares for the implementation of a new Page Builder widget. Analyzes widget requirements and design, validates them against Xperience documentation, and generates detailed implementation instructions."
argument-hint: "Path to the folder containing the user input files"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with preparing a detailed instructions file that will guide the implementation of a new Page Builder widget.

## Input Parameters

- **User Input Folder Path** - The user provided a path to the folder that contains user input files with requirements and design for the new widget. You must follow these when creating the final instructions file.

## Steps to follow

- First, check all documentation links in the `references/docs.md` file using Kentico Docs MCP.

- Next, read all remaining files in the `references/` folder.

- Then, check all requirements and design files in the user-input folder provided by the user.

- Check the current state of the project for resources you will need for creation of the widget. If you find already present widgets, follow their patterns and conventions.

- Finally, create a new instructions file in the user-input folder that will allow you to generate a new widget. Use `assets/CREATION_TEMPLATE.md` as a base and fill in all the parts in brackets. Other parts of the file must stay the same as in the template.
