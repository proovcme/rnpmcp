---
name: operate-renga-via-api
description: Inspect and safely modify an open Renga BIM model through the Renga API or a connected Renga MCP server. Use for querying model objects, creating or editing building geometry, managing styles and parameters, or diagnosing Renga automation.
---

# Operate Renga via API

Work against the currently open Renga project and treat the visible model as the source of truth.

## Choose the access path

- Prefer a connected Renga MCP server when it exposes the required read or write operation.
- For API functions missing from MCP, use a small x64 STA .NET helper with the official Renga interop assembly. Attach to the already running application through the Running Object Table; do not launch or close Renga unless asked.
- Use UI automation only for view/navigation tasks or final visual inspection, not as a substitute for API geometry when the API supports it.
- Read [references/api-basics.md](references/api-basics.md) before writing API code or diagnosing COM failures.
- Read [references/building-geometry.md](references/building-geometry.md) when creating walls, hosted openings, floors, or roofs.

## Safe workflow

1. Confirm that Renga has an open project and identify the target model and level.
2. Inspect existing objects, styles, host relationships, units, and object interfaces before changing anything.
3. State geometric assumptions that the user did not provide. Renga building coordinates and dimensions are normally millimetres.
4. Use a project operation for every mutation. Prefer an undoable operation tied to the target model.
5. Keep an operation small and coherent. Roll it back on any error; only apply after validating every required object and return value.
6. Re-query IDs, counts, hosts, and key parameters after applying.
7. Visually inspect the result in an appropriate plan and 3D view. Check actual placement, openings, joins, elevations, and roof edges rather than relying only on successful API calls.
8. Do not save, export, or overwrite the project unless the user asked. Report how to undo the applied change.

For destructive replacements, first resolve the exact target set. Remember that deleting a host may delete dependent objects. Preserve unrelated objects and styles.

## Quality bar

API success is not model success. For architecture, check clear dimensions, usable doors and windows, wall joins, roof closure and overhangs, and a clean silhouette. If the task is more than a schematic exercise, also apply the project-level ergonomic, accessibility, fire-safety, and engineering-space requirements.

When an operation is only a preview, label it clearly and leave the model unchanged. When the user asked to see a result in Renga, apply it and show the live model; do not silently roll it back.
