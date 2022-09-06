namespace Content.Shared.Spawners.Components;

/// <summary>
/// Put this component on something you would like to despawn after a certain amount of time
/// </summary>
[RegisterComponent]
public sealed class TimedDespawnComponent : Component
{
    /// <summary>
    /// How long the entity will exist before despawning
    /// </summary>
    [ViewVariables]
    [DataField("lifetime")]
    public float Lifetime = 5f;
}
