---
name: renga-inspect-model-data
description: Inspect and extract structured data and geometry from an open Renga project. Use for object inventories, parameters, quantities, custom properties, materials, reinforcement data, stable identifiers, mesh export, bounds, and evidence-backed model audits.
---

# Inspect Renga model data

Extract typed, traceable model evidence without confusing parameters, quantities, properties, or geometry.

## Read the relevant reference

- Read [references/model-data.md](references/model-data.md) for object identity, typed values, properties, quantities, and materials.
- Read [references/geometry-export.md](references/geometry-export.md) when bounds, collision evidence, mesh data, or material-grouped geometry is needed.

## Workflow

1. Confirm the connected process, API version, open project, and target model.
2. Define the requested population using type GUIDs and stable IDs; do not rely on localized display names alone.
3. Read values through their declared types and explicit units. Preserve unset, missing, unsupported, and zero as distinct states.
4. Paginate large object sets and retain provenance: model ID, object `UniqueId`, local ID, object type, source container, API version, and extraction time when relevant.
5. For geometry, choose object-grouped export when identity matters and material-grouped grids when rendering/material aggregation matters.
6. Cross-check surprising values against an object's additional interface, visible model, and official version documentation.
7. Keep the task read-only unless the user explicitly asks to register, assign, or change a property or parameter. Mutations require a project operation and post-write verification.

## Quality bar

- Never present a parameter as a measured quantity or a custom property as an intrinsic Renga value.
- Never infer units from a localized name.
- Never treat a local integer ID as durable across projects or sessions.
- Never claim exact collision or code compliance from AABB results alone.
- Report omissions, truncation, unsupported interfaces, and objects without requested data.
