---
description: "Prompt that helps with preparation of Widget creation process."
argument-hint: "userInputFolderPath"
tools:
  [
    "edit",
    "search",
    "runCommands",
    "kentico.docs.mcp/*",
    "problems",
    "fetch",
    "todos",
  ]
---

You are tasked with the process of creating a new prompt for generating a new widget.

## Input Parameters

- **User Input Folder Path:** `${input:userInputFolderPath}` - The path to the folder that contains user input files with requirements and design for the new widget. You must follow these when creating the final prompt.

## Steps to follow

- First, check all documentation links in the `./instructions/docs.instructions.md` file using Kentico Docs MCP.

- Next, read all remaining files in the `./instructions/` folder.

- Then, check all requirements and design files in the user-input folder, on path `${input:userInputFolderPath}`.

- Check the current state of the project for resources you will need for creation of the widget. If you find already present widgets, follow their patterns and conventions.

- Finally, create a new instructions file in the user-input folder that will allow you to generate a new widget. Use `./create-instructions/CREATION_TEMPLATE.instructions.md` as a base and fill in all the parts in brackets. Other parts of the file must stay the same as in the template.
