# Blazor Bundle Creator

This util is a quick side project aimed at reducing my insanity while creating Blazor files. Have to manually add each code behind, css, and JS file has gotten on my nerves in large projects. I know you can extract code blocks to a code behind, but still... :(

## Features
- **One-Click Generation:** Create `.razor`, `.razor.cs`, `.razor.css`, and `.razor.js` simultaneously.
- **Smart Namespaces:** Automatically detects your project's namespace based on the folder structure.
- **Configurable Defaults:** Set your preferred files in `Tools > Options > Blazor Utils`.
- **Safety First:** Prompts you before overwriting any existing files.
- **Automatic Sync:** Built-in watcher handles renaming siblings when you rename the main Razor file.

## How to Use
1. Right-click any folder in your Blazor project.
2. Select **Add > Blazor Component Bundle...**
3. Name your component and toggle the files you need.
4. Hit Enter and get back to coding.

## Installation
Download from the [Visual Studio Marketplace](LINK_HERE) or install via the Extensions Manager in Visual Studio.