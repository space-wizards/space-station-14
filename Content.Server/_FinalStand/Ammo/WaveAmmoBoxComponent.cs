namespace Content.Server._FinalStand.Ammo;

[RegisterComponent]
public sealed partial class WaveAmmoBoxComponent : Component
{
    /// players that have already used this box this wave. Cleared on WaveEndedEvent.
    public readonly HashSet<EntityUid> UsedBy = new();

    /// false when the APC circuit is dead; prevents interaction.
    public bool Enabled = true;

    [DataField]
    public TimeSpan RefillDuration = TimeSpan.FromSeconds(3);
}
