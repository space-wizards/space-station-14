namespace Content.Server.Zombies;

/// <summary>
/// Indicates a zombie that is "alive", i.e not crit/dead.
/// Causes it to emote when damaged.
/// TODO: move this to generic EmoteWhenDamaged comp/system.
/// </summary>
[RegisterComponent]
public sealed class ActiveZombieComponent : Component
{
    /// <summary>
    /// What emote to preform.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string GroanEmoteId = "Scream";

    /// <summary>
    /// Minimum time between groans.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DamageGroanCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Chance to groan.
    /// </summary>
    public float DamageGroanChance = 0.5f;

    /// <summary>
    /// The last time the zombie groaned from taking damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastDamageGroan = TimeSpan.Zero;
}
