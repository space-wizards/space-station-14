namespace Content.Server.Zombies;

/// <summary>
/// Overrides the applied accent for zombies.
/// </summary>
[RegisterComponent]
public sealed partial class ZombieAccentOverrideComponent : Component
{
    [DataField("accent")]
    public string Accent = "zombie";
}
