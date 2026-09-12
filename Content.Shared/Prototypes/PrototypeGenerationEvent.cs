using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.Prototypes;

/// <summary>
/// Raised by <see cref="PrototypeGenerationSystem"/> when systems should
/// generate any dynamic YML, for example chemistry bottles from all reagents.
/// This event is raised after all static YML is processed, letting you reference
/// other prototypes and their data using <see cref="IPrototypeManager"/>
/// </summary>
/// <param name="Ents">A list holding all entity prototypes to generate.</param>
[ByRefEvent]
public readonly record struct PrototypeGenerationEvent(
    IPrototypeManager Prototypes,
    ISerializationManager Serialization,
    List<(string Id, EntityBuilder Builder)> Ents,
    List<PrototypeBuilder> Protos
)
{
    /// <summary>
    /// Add an entity with the specified ID and optional
    /// <see cref="EntityBuilder"/> data.
    /// </summary>
    /// <param name="id">
    /// The id to use. Must be unique among all entity prototypes, dynamic or not.
    /// </param>
    /// <param name="builder">
    /// A builder for any additional optional data for the entity, such as components.
    /// </param>
    public void AddEntity(string id, EntityBuilder builder)
    {
        Ents.Add((id, builder));
    }

    /// <summary>
    /// Add a prototype instance with the specified ID.
    /// </summary>
    /// <param name="id">
    /// The id to use. Must be unique among all prototypes of type <see cref="T"/>,
    /// dynamic or not.
    /// </param>
    /// <param name="proto">
    /// The prototype instance to add.
    /// </param>
    /// <param name="builder">
    /// A builder for any additional data for this prototype.
    /// </param>
    /// <typeparam name="T">The type of prototype to add.</typeparam>
    public PrototypeBuilder AddProto<T>(string id, T proto) where T : class, IPrototype
    {
        var mapping = (MappingDataNode) Serialization.WriteValue(proto);
        mapping["type"] = Prototypes.TryGetKindFrom<T>(out var kind)
            ? new ValueDataNode(kind)
            : throw new ArgumentException($"No prototype kind found with type {typeof(T)}");
        mapping[IdDataFieldAttribute.Name] = new ValueDataNode(id);
        var builder = new PrototypeBuilder(Serialization, mapping);
        Protos.Add(builder);
        return builder;
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
    /// <param name="comp">The instance of the component to add.</param>
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

/// <summary>
/// A representation of a prototype that should be generated
/// dynamically at runtime. The id for it is not held in this struct.
/// See <see cref="EntityBuilder"/> for entities.
/// </summary>
/// <param name="Parents">The parents of the id as an array, if any.</param>
public record PrototypeBuilder(
    ISerializationManager Serialization,
    MappingDataNode Data
)
{
    /// <summary>
    /// Adds some data to this prototype.
    /// </summary>
    /// <param name="key">The key for this data, for example "name".</param>
    /// <param name="data">The instance of the component to add.</param>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <returns>This entity builder with the added component.</returns>
    /// <remarks>This does not check for duplicates.</remarks>
    public PrototypeBuilder Add<T>(string key, T data)
    {
        Data[key] = Serialization.WriteValue(data);
        return this;
    }
}
