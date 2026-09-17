---
name: renga-develop-api-plugin
description: Design, implement, package, and troubleshoot an in-process Renga API plugin. Use for IPlugin lifecycle, .rndesc manifests, .NET 8 or C++ SDK setup, Renga UI actions, event subscriptions, installation, and plugin load diagnostics; not for an external MCP server.
---

# Develop a Renga API plugin

Build an in-process extension that follows Renga's loading and lifetime rules. Keep it distinct from external COM or MCP automation.

## Read the relevant guidance

- Read [references/plugin-lifecycle.md](references/plugin-lifecycle.md) before scaffolding, packaging, installing, or debugging a plugin.
- Read [references/ui-and-events.md](references/ui-and-events.md) before adding actions, panels, context menus, or event handlers.

## Workflow

1. Confirm that the requested feature needs an in-process plugin. Use `$operate-renga-via-api` for external automation and MCP connection work.
2. Inspect the installed Renga and SDK versions. Select a supported API version and either .NET 8 or C++ deliberately.
3. Scaffold the plugin DLL and matching `.rndesc` manifest. Keep all SDK paths machine-local and out of the public repository.
4. Implement `IPlugin.Initialize` and `IPlugin.Stop`. Retain long-lived application, action, and event-source objects as fields.
5. Add UI commands through Renga actions. Subscribe only to required events, prevent re-entrant mutations, and release every subscription before its source becomes invalid.
6. Keep callbacks short. Move long computation outside immediate UI event handling when the API contract permits, and surface recoverable errors to the user.
7. Build and stage the plugin in an isolated folder. Install only after resolving the exact Renga installation and plugin target.
8. Restart Renga, verify load and unload, exercise each command, inspect `AecApp.log`, and test project open/close cycles.

## Public repository rules

- Do not commit Renga SDK files, `RengaCOMAPI.tlb`, generated Interop assemblies, `Renga.NET*.PluginUtility.dll`, Renga binaries, logos, or copied proprietary samples.
- Commit original source, manifest templates, build instructions, and tests only.
- Do not publish local installation paths, model contents, project identifiers, or logs containing user data.
- State that the integration is unofficial and that users must obtain Renga and the SDK under their own applicable terms.

## Boundaries

- A plugin is loaded into the Renga process at application startup; do not describe it as a hot-pluggable MCP command.
- Do not save, close, publish, or synchronize a project unless the user explicitly requested that external effect.
- Do not treat a successful `Initialize` return as proof that commands, subscriptions, unloading, or version compatibility work.
