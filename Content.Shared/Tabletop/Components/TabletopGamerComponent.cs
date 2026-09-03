using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Component for marking an entity as currently playing a tabletop.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TabletopSystem))]
public sealed partial class TabletopGamerComponent : Component
{
    /// <summary>
    /// The tabletop this entity is playing on.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid Tabletop;
}
