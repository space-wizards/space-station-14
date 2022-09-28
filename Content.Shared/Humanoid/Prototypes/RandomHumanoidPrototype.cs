using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype("randomHumanoid")]
public sealed class RandomHumanoidPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("randomizeName")] public bool RandomizeName { get; } = true;

    /// <summary>
    ///     Species that will be ignored by the randomizer.
    /// </summary>
    [DataField("speciesBlacklist")] public HashSet<string> SpeciesBlacklist { get; } = new();

    /// <summary>
    ///     Extra components to add to this entity.
    /// </summary>
    [DataField("components")]
    public EntityPrototype.ComponentRegistry? Components { get; }
}
