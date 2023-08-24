using Content.Server.Worldgen.Systems.Biomes;
using Content.Server.Worldgen.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

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
    [DataField("biomes", required: true,
        customTypeSerializer: typeof(PrototypeIdListSerializer<BiomePrototype>))] public List<string> Biomes = new();
}

