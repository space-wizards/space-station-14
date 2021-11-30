using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, ITileDefinition
    {
        [ViewVariables]
        string IPrototype.ID => Name;

        public string Path => "/Textures/Tiles/";

        [DataField("name", required: true)] public string Name { get; } = string.Empty;

        public ushort TileId { get; private set; }

        [DataField("display_name")] public string DisplayName { get; } = string.Empty;

        [DataField("texture")] public string SpriteName { get; } = string.Empty;

        [DataField("is_subfloor")] public bool IsSubFloor { get; private set; }

        [DataField("base_turfs")] public List<string> BaseTurfs { get; } = new();

        [DataField("can_crowbar")] public bool CanCrowbar { get; private set; }

        [DataField("footstep_sounds")] public SoundSpecifier? FootstepSounds { get; }

        [DataField("friction")] public float Friction { get; set; }

        [DataField("thermalConductivity")] public float ThermalConductivity { get; set; } = 0.05f;

        [DataField("item_drop", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemDropPrototypeName { get; } = "FloorTileItemSteel";

        [DataField("is_space")] public bool IsSpace { get; private set; }
        [DataField("sturdy")] public bool Sturdy { get; private set; } = true;

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
