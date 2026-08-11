namespace Content.Shared.Prototypes;

/// <summary>
/// Raised by <see cref="PrototypeGenerationSystem"/> when systems should
/// generate any dynamic YML, for example chemistry bottles from all reagents.
/// </summary>
/// <param name="Ents">A list holding all entity prototypes to generate.</param>
[ByRefEvent]
public readonly record struct PrototypeGenerationEvent(List<(string Id, EntityBuilder Builder)> Ents)
{
    /// <summary>
    /// Add an entity with the specified ID and optional
    /// <see cref="EntityBuilder"/> data.
    /// </summary>
    /// <param name="id">
    /// The id to use. Must be unique among all entity prototypes,
    /// dynamic or not.
    /// </param>
    /// <param name="builder">
    /// A builder for any additional optional data for the entity, such as components.
    /// </param>
    public void AddEntity(string id, EntityBuilder builder)
    {
        Ents.Add((id, builder));
    }
}

/// <summary>
/// A representation of an entity prototype that should be generated
/// dynamically at runtime. The id for it is not held in this struct.
/// </summary>
/// <param name="Parents">The parents of the id as an array, if any.</param>
/// <param name="Name">The name of the entity, if any.</param>
/// <param name="Description">The description of the entity, if any.</param>
/// <param name="Suffix">The suffix of the entity, if any.</param>
/// <param name="Components">The components of the entity, if any.</param>
public record struct EntityBuilder(
    string[]? Parents,
    string? Name,
    string? Description,
    string? Suffix,
    List<Component>? Components
)
{
    /// <summary>
    /// Adds a component instance to this entity.
    /// </summary>
    /// <param name="comp">The instnace of the component to add.</param>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <returns>This entity builder with the added component.</returns>
    /// <remarks>This does not check for duplicates.</remarks>
    public EntityBuilder AddComp<T>(T comp) where T : Component
    {
        Components ??= new List<Component>();
        Components.Add(comp);
        return this;
    }
}
