// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.StationDnd;

[Prototype("dndPlanet")]
public sealed partial class DndPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [ViewVariables]
    [DataField("minStructureDistance")]
    public readonly int MinStructureDistance = 80;

    [ViewVariables]
    [DataField("maxStructureDistance")]
    public readonly int MaxStructureDistance = 160;

    [ViewVariables]
    [DataField("mapMaxDistance")]
    public readonly int MapMaxDistance = 200;

    [ViewVariables]
    [DataField("biome", required: true)]
    public readonly ProtoId<BiomeTemplatePrototype> BiomePrototype = default!;

    [ViewVariables]
    [DataField("lightColor")]
    public readonly Color? LightColor = null;

    [ViewVariables]
    [DataField("structures")]
    public readonly Dictionary<ProtoId<DungeonConfigPrototype>, int> Structures = new();

    [ViewVariables]
    [DataField("spawnBase", required: true)]
    public readonly ProtoId<DungeonConfigPrototype> SpawnBase;

    [ViewVariables]
    [DataField("gravity")]
    public readonly bool Gravity = true;

    [ViewVariables]
    [DataField("atmosphere")]
    public readonly GasMixture? Atmosphere = null;

    [ViewVariables]
    [DataField("lootLayers")]
    public List<ProtoId<BiomeMarkerLayerPrototype>> LootLayers = new()
    {
        "OreIron",
        "OreQuartz",
        "OreGold",
        "OreSilver",
        "OrePlasma",
        "OreUranium",
        "OreArtifactFragment",
    };

    [ViewVariables]
    [DataField("nameDataset")]
    public ProtoId<LocalizedDatasetPrototype> NameDataset = "NamesBorer";
}
