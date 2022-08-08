namespace Content.Server.Revenant;

[RegisterComponent]
public sealed class EssenceComponent : Component
{
    /// <summary>
    /// Whether or not the entity has been harvested yet.
    /// </summary>
    [ViewVariables]
    public bool Harvested = false;

    /// <summary>
    /// Whether or not a revenant has searched this entity
    /// for its soul yet.
    /// </summary>
    [ViewVariables]
    public bool SearchComplete = false;

    /// <summary>
    /// The total amount of Essence that the entity has.
    /// Changes based on mob state.
    /// </summary>
    [ViewVariables]
    public float EssenceAmount = 0f;
}
