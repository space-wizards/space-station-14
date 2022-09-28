using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype("randomHumanoid")]
public sealed class RandomHumanoidPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<RandomHumanoidPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    public bool Abstract { get; }

    [DataField("randomizeName")] public bool RandomizeName { get; } = true;

    /// <summary>
    ///     Species that will be ignored by the randomizer.
    /// </summary>
    [DataField("speciesBlacklist")]
    public HashSet<string> SpeciesBlacklist { get; } = new();

    /// <summary>
    ///     Extra components to add to this entity.
    /// </summary>
    [DataField("components")]
    public EntityPrototype.ComponentRegistry? Components { get; }
}
