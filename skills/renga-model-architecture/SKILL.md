---
name: renga-model-architecture
description: Create, edit, and validate architectural model geometry in an open Renga project through the API or Renga MCP. Use for levels, walls, floors, roofs, rooms, doors, windows, fixtures, furniture, hosts, placement, and collision-aware layout work.
---

# Model architecture in Renga

Build architecture that is usable and geometrically coherent, not merely accepted by the API.

## Read the relevant guidance

- Read [references/creation-and-placement.md](references/creation-and-placement.md) before creating or moving model objects.
- Read [references/architectural-validation.md](references/architectural-validation.md) before applying a multi-object layout or reporting completion.

## Workflow

1. Inspect the open project, target building model, levels, styles, existing hosts, and object interfaces.
2. Translate the request into explicit levels, host relationships, contours, placements, elevations, dimensions, and style choices. Mark missing design inputs as assumptions.
3. Create primary hosts before dependants: levels and main geometry, then hosted openings and equipment, then rooms and contents.
4. Use a small undoable operation tied to the building model. If a newly created host cannot be used until applied, split the work into consecutive undoable operations and re-query IDs between them.
5. Validate returned objects and specialized interfaces before apply. After apply, re-query placement and exported geometry because Renga may project or normalize input placement.
6. Inspect plan and 3D views. Check hosts, joins, elevations, roof closure, room usability, doors, windows, furniture, fixtures, and service assumptions.
7. Do not save the project unless requested. Report assumptions, remaining schematic approximations, and how many undo items the change created.

## Boundaries

- Never delete or replace existing model content until the exact target set and dependent-object impact are known.
- Do not infer a style's physical dimensions from its name or insertion point.
- Do not claim code-compliance from API success, absence of warnings, or collision-free bounding boxes.
- When the user wants to see the result, apply the model changes and show the live project; a rolled-back preview does not satisfy that request.
