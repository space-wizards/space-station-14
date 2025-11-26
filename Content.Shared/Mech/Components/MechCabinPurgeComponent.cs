using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechCabinPurgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CooldownRemaining;

    /// <summary>
    /// Total cooldown duration applied after a purge, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CooldownDuration = 3f;
}
