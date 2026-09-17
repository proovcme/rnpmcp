---
name: operate-renga-via-api
description: Connect to and operate Renga through its COM API or a Renga MCP server. Use for ROT attachment, application and project lifecycle, transaction handling, version compatibility, or diagnosing COM automation. Use the specialized Renga skills for architecture modeling or model-data extraction.
---

# Operate Renga via API

Connect to Renga predictably, select the correct project model, and keep every mutation recoverable.

## Route the task

- Use this skill for connection, process selection, project lifecycle, operations, undo stacks, and COM failures. Read [references/api-basics.md](references/api-basics.md).
- Use `$renga-model-architecture` for levels, walls, openings, floors, roofs, rooms, furniture, and architectural quality checks.
- Use `$renga-inspect-model-data` for parameters, quantities, properties, materials, reinforcement, or exported mesh geometry.
- Treat plugins loaded into Renga as a different runtime from an external MCP or helper. Do not apply plugin deployment or UI-extension patterns to an out-of-process controller.

## Choose the access path

- Prefer the connected Renga MCP server when it exposes the required operation.
- To control the user's already open Renga, attach through the Running Object Table and choose the human-readable moniker by API version and PID. Do not instantiate `Renga.Application.1`: COM activation may launch another process.
- For API functions missing from MCP, use a small x64 STA .NET helper. Typed interop is more reliable for GUIDs, structures, SAFEARRAYs, and additional interfaces; late-bound COM avoids redistributing Renga SDK artifacts.
- Use UI automation for navigation or final visual inspection when needed, not to emulate API geometry operations.

## Safe workflow

1. Confirm the selected Renga process, API version, open-project state, and project type.
2. Resolve the target model: building, assembly, and drawing models have different IDs and undo stacks.
3. Inspect `Project.HasActiveOperation()` before starting a mutation. Never nest operations.
4. For model objects, prefer `StartOperationWithUndo(model.Id)` or `CreateOperationWithUndo(model.Id)` followed by `Start()`. Project entities such as styles and materials do not participate in model undo.
5. Keep an operation small and coherent. Check every returned object and result code, roll back on any failure, and apply only once validation succeeds.
6. Release or discard cached COM wrappers after apply and re-query durable results. Use `UniqueId` for references across sessions; local integer IDs are model-local and session-oriented.
7. Do not save, export, close a project, quit Renga, publish, or synchronize unless the user explicitly requested that external effect.

For destructive replacements, first resolve the exact target set. Deleting a host may delete dependants. Preserve unrelated objects and report the owning undo stack.

## Quality bar

API success proves only that the call completed. Verify the requested application or project state after every operation and surface `LastError`, nonzero result codes, missing interfaces, or version-dependent fallbacks.

When an operation is only a preview, label it clearly and leave the model unchanged. When the user asked to see a result in Renga, apply it and show the live model; do not silently roll it back.
