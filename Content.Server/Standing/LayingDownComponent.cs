namespace Content.Server.Standing;

[RegisterComponent]
public sealed partial class LayingDownComponent : Component
{
    [DataField]
    public float DownedSpeedMultiplier = 0.15f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2.5f);

    [DataField]
    public TimeSpan NextToggleAttempt = TimeSpan.Zero;
}
