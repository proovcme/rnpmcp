# EngineeringObjectConnector workflow

Use this reference for API mechanics. Complete the spatial and engineering checks in [mep-validation.md](mep-validation.md) before reporting success.

## Connection sequence

1. Obtain `IEngineeringObjectConnector` from `IProject.EngineeringObjectConnector`.
2. Create the matching parameter object:
   - `CreatePipeSystemConnectionParameters()`;
   - `CreateDuctSystemConnectionParameters()`;
   - `CreateElectricalSystemConnectionParameters()`.
3. Resolve source and target objects and valid zero-based port indexes.
4. Choose a `SystemCategory` available on the selected ports.
5. Configure compatible styles and routing parameters.
6. Start an `IProject.StartOperation()`, call the matching `Create*SystemConnection`, validate its result, then `Apply()` or `RollBack()`.
7. Re-query route and fitting objects after apply; never infer creation from lack of an exception.

In this repository's MCP server, use `renga_get_ports`, `renga_system_categories`, `renga_list_styles`, and `renga_create_pipe_connection` in that order. The pipe connection tool defaults to `preview=true`, compares the model snapshots before and after routing, and rolls the preview back. Commit only with `preview=false` after reviewing the returned generated objects.

Connection creation is synchronous. Complex routing can freeze or blacken the Renga UI until the call completes. Keep the requested scope bounded and warn before expensive batches.

## Ports and endpoints

- Use valid zero-based port indices for model objects with ports.
- A pipe or duct route may be one endpoint. Pass `-1` for the route side because its port index is ignored.
- Route-to-route connection is unsupported.
- Do not connect the same object and same port to itself.
- A port that is already connected cannot start another connection unless the model and object type explicitly support another available port.
- Electrical connection creation is limited to objects with ports. Targeting an existing electrical route does not create a second connection.
- `SystemCategory` must match port availability. A mismatch can yield no new route without a thrown exception.

## Pipe and duct parameters

Pipe and duct parameter objects distinguish main and branch rules. Set deliberately:

- `HeightMagistral` and `HeightBranch`;
- `OffsetMagistral` and `OffsetBranch`;
- `ConsiderEnclosingStructuresMagistral` and `ConsiderEnclosingStructuresBranch`;
- compatible route styles for main and branch;
- compatible fitting styles;
- an insulation material ID, where `0` explicitly means no insulation.

Fittings must be compatible with route styles in material, connection type, and connection geometry. Treat all numeric values as Renga API base units and verify the relevant property contract before writing.

Do not assume that a same-diameter fitting name proves compatibility. On Renga 9.3 / API 2.50, an over-constrained or partly incompatible fitting preference list can fail with `Invalid fitting components`, while the same connection succeeds when the fitting list is omitted and Renga selects a compatible result. Start with a reviewed route style and the smallest justified fitting set; if the preview reports this error, retry once with the fitting preferences removed, then inspect the generated route and fittings before commit.

## Electrical parameters

Set the documented height, offset, enclosing-structure behaviour, and compatible electrical circuit line styles. Do not transpose pipe or duct assumptions to electrical routing.

## Failure diagnosis

Before the operation, clear `IApplication.LastError` when available. If nothing is created, inspect it immediately and check, in order:

1. endpoint object types and port indexes;
2. whether either port is already occupied;
3. system-category compatibility;
4. route and fitting style compatibility;
5. insulation ID and other referenced IDs;
6. height, offset, and enclosing-structure parameters;
7. unsupported route endpoint combinations.

After a successful connection, re-read every fixture port. A route point intentionally used as a project boundary can retain a free port; do not disguise that boundary by mixing unrelated system categories or closing a fictitious loop merely to remove a warning marker.

After apply, compare object collections or stable identifiers before and after the operation. A non-null dispatch result alone is not a topology audit.

## Official reference

- [Renga API: How to use EngineeringObjectConnector](https://help.rengabim.com/api/how-to-use-engineering-object-connector.html)
- [Renga API: IProject](https://help.rengabim.com/api/interface_i_project.html)
- [Renga API: IOperation](https://help.rengabim.com/api/interface_i_operation.html)
