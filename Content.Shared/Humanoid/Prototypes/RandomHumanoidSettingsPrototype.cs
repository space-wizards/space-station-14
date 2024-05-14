using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
///     This is what is used to change a humanoid spawned by RandomHumanoidSystem in Content.Server.
/// </summary>
[Prototype("randomHumanoidSettings")]
public sealed partial class RandomHumanoidSettingsPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<RandomHumanoidSettingsPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; }

    /// <summary>
    ///     Whether the humanoid's name should take from the randomized profile or not.
    /// </summary>
    [DataField("randomizeName")]
    public bool RandomizeName { get; private set; } = true;

    /// <summary>
    ///     Species that will be ignored by the randomizer.
    /// </summary>
    [DataField("speciesBlacklist")]
    public HashSet<string> SpeciesBlacklist { get; private set; } = new();

    /// <summary>
    ///     Extra components to add to this entity.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry? Components { get; private set; }
}
