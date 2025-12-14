using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for managing special plant traits and mutations.
/// </summary>
/// TODO: The logic for these component is quite hardcoded.
/// They require a separate a system that will use events or APIs from other growth systems.
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantTraitsComponent : Component
{
    /// <summary>
    /// If true, produce can't be put into the seed maker.
    /// </summary>
    [DataField]
    public bool Seedless = false;

    /// <summary>
    /// If true, a sharp tool is required to harvest this plant.
    /// </summary>
    [DataField]
    public bool Ligneous = false;

    /// <summary>
    /// If true, the plant can scream when harvested.
    /// </summary>
    [DataField]
    public bool CanScream = false;

    /// <summary>
    /// If true, the plant can turn into kudzu.
    /// </summary>
    [DataField]
    public bool TurnIntoKudzu = false;

    /// <summary>
    /// Which kind of kudzu this plant will turn into if it kuzuifies.
    /// </summary>
    [DataField]
    public EntProtoId KudzuPrototype = "WeakKudzu";

    /// <summary>
    /// If false, rapidly decrease health while growing. Adds a bit of challenge to keep mutated plants alive via Unviable's frequency.
    /// </summary>
    [DataField]
    public bool Viable = true;
}
