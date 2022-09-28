using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid.Components;

/// <summary>
///     This is added to a marker entity in order to spawn a randomized
///     humanoid ingame.
/// </summary>
[RegisterComponent]
public sealed class RandomHumanoidComponent : Component
{
    [DataField("settings")] public string RandomSettingsId = default!;
}

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
