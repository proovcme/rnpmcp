# UI actions and event safety

Use this reference whenever a plugin adds commands or reacts to Renga events.

## UI model

- Obtain `IUI` from the application.
- Renga exposes commands as `IAction` objects rather than direct manipulation of tool buttons or windows.
- One action may back several controls. Define display name, tooltip, icon, enabled state, and event handling on the action.
- Add actions through a primary-panel extension, a view-specific actions-panel extension, a context menu, or another documented UI host.
- Give persistent context menus and related extensions stable unique GUIDs. Reusing the same ID is how an existing context menu is updated; accidental reuse can replace another extension.
- Load icons and other resources relative to the plugin folder. Handle missing resources without crashing initialization.

## Event-source lifetime

- SDK `...EventSource` helpers wrap COM event subscriptions. Store each event source as a field for as long as its events are needed.
- Destroying an event source removes its subscriptions. Conversely, keeping one after the underlying source object becomes invalid can cause crashes or undefined behaviour.
- In C#, unsubscribe named delegates when appropriate and call `Dispose()` on event sources in `Stop()`. In C++, retain subscription IDs where selective unsubscription is needed and destroy event sources before their sources.
- Project-, selection-, view-, and object-scoped subscriptions may become invalid before application shutdown. Use relevant `Before...` events or project-close handling to tear them down early.
- Avoid anonymous callbacks when later selective unsubscription is required.

## Callback rules

- Do not mutate a source from its own event handler in a way that raises the same event again. Guard unavoidable feedback loops explicitly.
- Validate that a project and the required model still exist when the callback runs.
- Keep event and action callbacks bounded. Renga UI work is synchronous; expensive routing, scanning, or export can make the application appear frozen.
- Catch and report expected failures at command boundaries, but do not suppress evidence needed for diagnosis.
- Make repeated commands idempotent where practical and prevent duplicate UI registration or duplicate subscriptions.

## Verification

Test more than the happy path:

- action appears only in intended contexts and its enabled state updates;
- repeated triggering does not duplicate work or handlers;
- project close/open and view changes do not leave stale sources;
- plugin unload disposes all sources cleanly;
- an event callback cannot recursively trigger itself;
- failures leave the model and UI in a recoverable state.

## Official references

- [Renga API: How to extend the user interface](https://help.rengabim.com/api/how-to-extend-ui.html)
- [Renga API: How to handle events](https://help.rengabim.com/api/how-to-handle-events.html)
