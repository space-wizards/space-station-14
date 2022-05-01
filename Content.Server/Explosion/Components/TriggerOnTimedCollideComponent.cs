namespace Content.Server.Explosion.Components;

/// <summary>
/// Triggers when the entity is overlapped
/// </summary>
[RegisterComponent]
public sealed class TriggerOnTimedCollideComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("threshold")]
    public float Threshold;

    [ViewVariables]
    public readonly Dictionary<EntityUid, float> Colliding = new();
}
