using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Component for marking an entity as currently playing a tabletop.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTabletopSystem))]
public sealed partial class TabletopGamerComponent : Component
{
    /// <summary>
    /// The tabletop this entity is playing on.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid Tabletop = EntityUid.Invalid;

    /// <summary>
    /// If true, this entity is using the upside down camera.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool UpsideDown;
}
