# Renga model data semantics

Official sources:

- https://help.rengabim.com/api/how-to-work-with-model-object.html
- https://help.rengabim.com/api/how-to-parameters.html
- https://help.rengabim.com/api/how-to-obtain-quantities.html
- https://help.rengabim.com/api/how-to-properties.html
- https://help.rengabim.com/api/how-to-object-parametric-representation.html
- https://help.rengabim.com/api/how-to-reinforcement.html

## Identity and traversal

Building, assembly, and drawing models are separate `IModel` instances. Within a model, objects have a local integer `Id`, stable `UniqueId`, and `ObjectType` GUID. Use type GUIDs for filtering and `UniqueId` for durable references. Names are localized labels and are unsuitable as primary keys.

Additional capabilities are exposed through interfaces rather than inferred from object type alone. In typed COM, query or cast the interface. In late-bound clients, use `GetInterfaceByName()`.

## Parameters, quantities, and properties

These containers have different meanings:

- A **parameter** belongs to an object's parametric representation and can be writable. Availability can depend on representation and version.
- A **quantity** is a read-only derived physical measure. Use `Contains()` before `Get()` and then the accessor matching the quantity type.
- A **property** is arbitrary project metadata registered and assigned to an entity type, style, or supported entity. Renga stores it but does not interpret its business meaning.

Do not merge these categories in reports. Include source kind and identifier for every value.

## Typed values and units

Inspect the definition or runtime type before reading or writing. Use the matching typed accessor. Renga exposes explicit unit arguments for many lengths, areas, volumes, masses, and angles; request the unit required by the deliverable and record it.

Distinguish:

- container does not contain the requested ID;
- value exists but is unset;
- value is an empty string;
- numeric value is zero;
- interface or accessor is unsupported in the installed API version.

Enumeration values may require the related definition or item collection to translate stored values. Do not invent labels from integer ordinals.

## Custom-property mutation

Registering, assigning, or changing a property is a project mutation:

1. choose a stable new GUID under the caller's namespace;
2. inspect existing registrations and names to avoid semantic duplicates;
3. start a regular project operation;
4. register the typed property and assign it only to intended entity types;
5. set values through each object's `IPropertyContainer`;
6. apply, re-query, and verify type, assignment, and value.

Project properties are not model objects, so a model undo stack may not cover the entire change. Do not unregister or reassign properties without understanding the effect on existing values.

## Materials and styles

Resolve style and material IDs through the project's entity collections. ID zero can mean “none” for documented collections. A style parameter is not necessarily an object parameter; report which entity owns the value.

For material takeoffs, reconcile object quantities, assigned materials or layered materials, and exported mesh materials. These represent different levels of the model and need not produce identical groupings.

## Reinforcement

Obtain `IObjectReinforcementModel` only from supported objects. Rebar usages group equal geometry and style within an owner. Quantities such as total length and mass belong to each usage; geometry must be transformed through the placements returned for that usage. Do not sum nested assembly reinforcement twice.
