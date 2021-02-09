using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, IIndexedPrototype, ITileDefinition
    {
        string IIndexedPrototype.ID => Name;

        [YamlField("name")] public string Name { get; private set; }
        public ushort TileId { get; private set; }
        [YamlField("display_name")] public string DisplayName { get; private set; }
        [YamlField("texture")] public string SpriteName { get; private set; }
        [YamlField("is_subfloor")] public bool IsSubFloor { get; private set; }
        [YamlField("base_turfs")] public List<string> BaseTurfs { get; private set; } = new();
        [YamlField("can_crowbar")] public bool CanCrowbar { get; private set; }
        [YamlField("footstep_sounds")] public string FootstepSounds { get; private set; }
        [YamlField("friction")] public float Friction { get; set; }
        [YamlField("thermalConductivity")] public float ThermalConductivity { get; set; } = 0.05f;
        [YamlField("item_drop")] public string ItemDropPrototypeName { get; private set; } = "FloorTileItemSteel";
        [YamlField("is_space")] public bool IsSpace { get; private set; }

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
