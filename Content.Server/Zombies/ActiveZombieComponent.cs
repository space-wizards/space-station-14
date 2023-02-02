namespace Content.Server.Zombies;

[RegisterComponent]
public sealed class ActiveZombieComponent : Component
{
    /// <summary>
    /// The chance that on a random attempt
    /// that a zombie will do a groan
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float GroanChance = 0.2f;

    /// <summary>
    /// Minimum time between groans
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float GroanCooldown = 2;

    /// <summary>
    /// The length of time between each zombie's random groan
    /// attempt.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomGroanAttempt = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    public string GroanEmoteId = "Scream";

    [ViewVariables(VVAccess.ReadWrite)]
    public float LastDamageGroanCooldown = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0f;
}
