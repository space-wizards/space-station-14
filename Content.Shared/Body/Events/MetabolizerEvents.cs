namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity to determine their metabolic multiplier.
/// </summary>
[ByRefEvent]
public record struct GetMetabolicMultiplierEvent()
{
    /// <summary>
    /// What the metabolism's update rate will be multiplied by.
    /// </summary>
    public float Multiplier = 1f;
}

/// <summary>
/// Raised on an entity to apply their metabolic multiplier to relevant systems.
/// Note that you should be storing this value as to not accrue precision errors when it's modified.
/// </summary>
[ByRefEvent]
public readonly record struct ApplyMetabolicMultiplierEvent(float Multiplier)
{
    /// <summary>
    /// What the metabolism's update rate will be multiplied by.
    /// </summary>
    public readonly float Multiplier = Multiplier;
}
