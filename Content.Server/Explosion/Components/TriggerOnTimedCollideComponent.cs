namespace Content.Server.Explosion.Components;

/// <summary>
/// Triggers when the entity is overlapped for the specified duration.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnTimedCollideComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("threshold")]
    public float Threshold;

    /// <summary>
    /// A collection of entities that are colliding with this, and their own unique accumulator.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<EntityUid, float> Colliding = new();
}
