using Content.Server.Worldgen.Prototypes;
using Content.Server.Worldgen.Systems.Biomes;
using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Components;

/// <summary>
///     This is used for selecting the biome(s) to be used during world generation.
/// </summary>
[RegisterComponent]
[Access(typeof(BiomeSelectionSystem))]
public sealed partial class BiomeSelectionComponent : Component
{
    /// <summary>
    ///     The list of biomes available to this selector.
    /// </summary>
    /// <remarks>This is always sorted by priority after ComponentStartup.</remarks>
    [DataField(required: true)]
    public List<ProtoId<BiomePrototype>> Biomes = new();
}

