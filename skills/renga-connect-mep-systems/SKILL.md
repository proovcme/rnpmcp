---
name: renga-connect-mep-systems
description: Create and diagnose pipe, duct, and electrical system connections in Renga through IEngineeringObjectConnector. Use for ports, system categories, route styles, fittings, insulation, routing parameters, topology checks, and coordinated MEP validation.
---

# Connect MEP systems in Renga

Create actual, inspectable system topology. An API call that returns without throwing is not proof that a route exists or is usable.

## Read the relevant guidance

- Read [references/engineering-connector.md](references/engineering-connector.md) before every creation or repair operation.
- Read [references/mep-validation.md](references/mep-validation.md) before applying a route or reporting completion.

## Workflow

1. Inspect the open project, endpoint objects, all available ports, current connections, system categories, route styles, fitting styles, insulation materials, levels, and enclosing structures.
2. Establish the design intent: medium or circuit, source and target, flow or circuit direction when relevant, preferred elevation, main and branch rules, slope, service access, and permitted penetrations. Mark missing engineering inputs as assumptions.
3. Select compatible endpoint ports and a `SystemCategory` supported by both sides. Reject occupied ports, identical endpoints, invalid indexes, and unsupported route-to-route requests.
4. Build the correct pipe, duct, or electrical parameter object. Supply only compatible route and fitting styles and deliberate height, offset, insulation, and enclosing-structure settings.
5. Clear the application error state when available. Start one project operation, create the connection, inspect the immediate result, and apply only if the result is valid; otherwise roll back.
6. Re-query the model after apply. Confirm that new route objects and fittings exist, connect to the intended ports, carry the intended system category, and use the expected styles.
7. Validate geometry and use: continuity, clashes, slopes, bend feasibility, access, penetrations, ceiling and floor zones, equipment removal paths, and coordination with architecture and other systems.
8. Report created object IDs or stable identifiers, assumptions, warnings, remaining coordination tasks, and the undo impact. Do not save unless requested.

## Boundaries

- Use `$operate-renga-via-api` for connection, COM lifetime, and transaction diagnostics outside the connector itself.
- Connector routing is not engineering design. Use the relevant HVAC, plumbing, electrical, or fire-protection skill for calculations and design basis.
- Do not invent port compatibility, style IDs, diameters, elevations, or system categories from display names.
- Do not claim success until topology and post-apply model state have been re-read.
