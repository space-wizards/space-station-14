using Content.Shared.Polymorph.Systems;

namespace Content.Shared.Polymorph.Components;

/// <summary>
/// Marker component. If this component is present on a grid, players cannot use a Chameleon Projector while on that grid.
/// </summary>
[RegisterComponent, Access(typeof(SharedChameleonProjectorSystem))]
public sealed partial class ChameleonBlockerGridComponent : Component;
