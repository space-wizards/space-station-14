namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed partial class MechCabinPurgeComponent : Component
{
    [DataField]
    public float CooldownRemaining;

    /// <summary>
    /// Total cooldown duration applied after a purge, in seconds.
    /// </summary>
    [DataField]
    public float CooldownDuration = 3f;
}
