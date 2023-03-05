using Content.Server._Citadel.Worldgen.Systems.Biomes;

namespace Content.Server._Citadel.Worldgen.Components;

/// <summary>
///     This is used for selecting the biome(s) to be used during world generation.
/// </summary>
[RegisterComponent]
[Access(typeof(BiomeSelectionSystem))]
public sealed class BiomeSelectionComponent : Component
{
    /// <summary>
    ///     The list of biomes available to this selector.
    /// </summary>
    /// <remarks>This is always sorted by priority after ComponentStartup.</remarks>
    [DataField("biomes", required: true)] public List<string> Biomes = new();
}

