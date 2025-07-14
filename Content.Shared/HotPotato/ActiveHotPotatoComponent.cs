using Robust.Shared.GameStates;

namespace Content.Shared.HotPotato;

/// <summary>
/// Added to an activated hot potato. Controls hot potato transfer on server / effect spawning on client.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedHotPotatoSystem))]
public sealed partial class ActiveHotPotatoComponent : Component
{
    /// <summary>
    /// Hot potato effect spawn cooldown in seconds
    /// </summary>
    [DataField]
    public float EffectCooldown = 0.3f;

    /// <summary>
    /// Moment in time next effect will be spawned
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TargetTime = TimeSpan.Zero;
}
