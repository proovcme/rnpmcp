# Plugin lifecycle and packaging

Use this reference for project setup, manifests, installation, startup, shutdown, and load failures.

## Runtime model

- A Renga plugin is a Windows DLL or .NET assembly plus an XML description file with the `.rndesc` extension.
- Renga loads plugins into its own process on application startup and unloads them on exit. A rebuilt binary normally requires a Renga restart before it is exercised.
- Place each description file in its own subdirectory under the Renga `Plugins` directory. The folder and `.rndesc` base name must match.
- Treat plugin code as in-process code: an unhandled exception, stale COM event source, blocking callback, or invalid native dependency can destabilize the host application.

## Description file

Include the required elements:

- `Name`
- `Version`
- `Copyright`
- `RequiredAPIVersion`
- `PluginFilename`
- `Vendor`

Set `PluginType` explicitly. Use `Net8` for a .NET 8 plugin; the documented defaults and alternatives differ for .NET Framework and C++ plugins. Set `RequiredAPIVersion` to the minimum version actually used, not simply the developer's installed version.

Keep the binary path relative to the plugin folder when practical. Validate the XML and verify that `PluginFilename` resolves after installation.

## .NET setup

- Target a Windows-compatible class library and the architecture supported by the installed Renga.
- Reference the Renga COM type library from the locally installed SDK. The official guidance requires `Embed Interop Types` to be false.
- Reference the matching SDK utility library: `Renga.NET8.PluginUtility.dll` for .NET 8 or the documented .NET Framework variant when deliberately targeting that runtime.
- Keep SDK locations configurable through local build properties. Never vendor the type library, utility DLL, generated Interop assembly, or Renga binaries into a public repository.

## Lifecycle contract

Implement `Renga.IPlugin`:

- `bool Initialize(string pluginFolder)` creates application references, resources, actions, UI extensions, and subscriptions. Use `pluginFolder` to resolve packaged resources.
- `void Stop()` unsubscribes and disposes event sources, releases long-lived resources, and leaves no callbacks that can fire after unload.

Retain the application and any object whose lifetime controls behaviour as fields rather than short-lived locals. Return `false` from initialization when the plugin cannot enter a safe usable state.

## Installation and diagnosis

1. Resolve the target Renga installation explicitly; do not guess between editions or versions.
2. Stage the plugin in a temporary or build-output directory and verify that all original plugin dependencies are present.
3. Install the folder and matching `.rndesc` only into the intended Renga instance.
4. Restart Renga and inspect `%LOCALAPPDATA%\Renga Software\Renga\AecApp.log` if the plugin does not load.
5. Test clean startup, project creation/open/close, repeated command use, project switching, and application exit.

## Official references

- [Renga API: How to implement a plugin](https://help.rengabim.com/api/how-to-implement-a-plugin.html)
- [Renga API: IPlugin](https://help.rengabim.com/api/class_renga_1_1_i_plugin.html)
- [Renga API overview: SDK and plugins](https://help.rengabim.com/api/overview-api-sdk-plugin.html)
