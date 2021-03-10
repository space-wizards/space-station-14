#nullable enable
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, ITileDefinition
    {
        [ViewVariables]
        string IPrototype.ID => Name;

        [field: DataField("name", required: true)] public string Name { get; } = string.Empty;

        public ushort TileId { get; private set; }

        [field: DataField("display_name")] public string DisplayName { get; } = string.Empty;

        [field: DataField("texture")] public string SpriteName { get; } = string.Empty;

        [DataField("is_subfloor")] public bool IsSubFloor { get; private set; }

        [field: DataField("base_turfs")] public List<string> BaseTurfs { get; } = new();

        [DataField("can_crowbar")] public bool CanCrowbar { get; private set; }

        [field: DataField("footstep_sounds")] public string FootstepSounds { get; } = string.Empty;

        [DataField("friction")] public float Friction { get; set; }

        [DataField("thermalConductivity")] public float ThermalConductivity { get; set; } = 0.05f;

        [field: DataField("item_drop")] public string ItemDropPrototypeName { get; } = "FloorTileItemSteel";

        [DataField("is_space")] public bool IsSpace { get; private set; }

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
