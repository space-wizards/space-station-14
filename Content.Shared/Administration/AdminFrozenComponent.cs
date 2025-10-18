using Robust.Shared.GameStates;

namespace Content.Shared.Administration;

[RegisterComponent, Access(typeof(AdminFrozenSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdminFrozenComponent : Component
{
    /// <summary>
    /// Whether the player is also muted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Muted;
}
