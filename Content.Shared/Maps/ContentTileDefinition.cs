using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public readonly record struct ContentTileDefinition : IPrototype, IInheritingPrototype, ITileDefinition
    {
        public const string SpaceID = "Space";
        private readonly string _name = string.Empty;

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ContentTileDefinition>))]
        public string[]? Parents { get; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; }

        [IdDataFieldAttribute] public string ID { get; } = string.Empty;

        public ushort TileId { get; }

        [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        public string Name { get; } = string.Empty;

        [DataField("sprite")] public ResourcePath? Sprite { get; }

        [DataField("isSubfloor")] public bool IsSubFloor { get; }

        [DataField("baseTurfs")] public List<string> BaseTurfs { get; } = new();

        [DataField("canCrowbar")] public bool CanCrowbar { get; }

        [DataField("canWirecutter")] public bool CanWirecutter { get; }

        /// <summary>
        /// These play when the mob has shoes on.
        /// </summary>
        [DataField("footstepSounds")] public SoundSpecifier? FootstepSounds { get; }

        /// <summary>
        /// These play when the mob has no shoes on.
        /// </summary>
        [DataField("barestepSounds")] public SoundSpecifier? BarestepSounds { get; } = new SoundCollectionSpecifier("BarestepHard");

        [DataField("friction")] public float Friction { get; }

        [DataField("variants")] public byte Variants { get; } = 1;

        /// <summary>
        /// This controls what variants the `variantize` command is allowed to use.
        /// </summary>
        [DataField("placementVariants")]
        public byte[] PlacementVariants { get; } = new byte[1] { 0 };

        [DataField("thermalConductivity")] public float ThermalConductivity { get; } = 0.05f;

        // Heat capacity is opt-in, not opt-out.
        [DataField("heatCapacity")] public readonly float HeatCapacity = Atmospherics.MinimumHeatCapacity;

        [DataField("itemDrop", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemDropPrototypeName { get; } = "FloorTileItemSteel";

        [DataField("isSpace")] public bool IsSpace { get; }
        [DataField("sturdy")] public bool Sturdy { get; } = true;

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
