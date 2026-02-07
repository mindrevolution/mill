# mill Plugin

This folder contains the MCP auto-installer for the mill Claude Code plugin.

## How It Works

When users install the plugin:

```
/plugin marketplace add mindrevolution/mill-plugin
/plugin install mill@mindrevolution-mill-plugin
```

1. Claude Code clones the `mill-plugin` repo
2. Commands from `skills/` are installed automatically
3. The MCP server starts and checks if mill CLI is installed
4. If missing, it downloads and installs the latest release

## Files

- `mill-installer/index.mjs` - MCP server that auto-installs the CLI
- `mill-installer/package.json` - Package metadata

## Sync Process

This plugin is synced to `mindrevolution/mill-plugin` via GitHub Action on each release.
Source of truth is the main `mill` repo.
