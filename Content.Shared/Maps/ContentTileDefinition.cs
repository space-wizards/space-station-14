using Content.Shared.Atmos;
using Content.Shared.Light.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Maps
{
    [Prototype("tile")]
    public sealed partial class ContentTileDefinition : IPrototype, IInheritingPrototype, ITileDefinition
    {
        [ValidatePrototypeId<ToolQualityPrototype>]
        public const string PryingToolQuality = "Prying";

        public const string SpaceID = "Space";

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ContentTileDefinition>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        [IdDataField] public string ID { get; private set; } = string.Empty;

        public ushort TileId { get; private set; }

        [DataField("name")]
        public string Name { get; private set; } = "";
        [DataField("sprite")] public ResPath? Sprite { get; private set; }

        [DataField("edgeSprites")] public Dictionary<Direction, ResPath> EdgeSprites { get; private set; } = new();

        [DataField("edgeSpritePriority")] public int EdgeSpritePriority { get; private set; } = 0;

        [DataField("isSubfloor")] public bool IsSubFloor { get; private set; }

        [DataField("baseTurf")]
        public string BaseTurf { get; private set; } = string.Empty;

        [DataField]
        public PrototypeFlags<ToolQualityPrototype> DeconstructTools { get; set; } = new();

        /// <remarks>
        /// Legacy AF but nice to have.
        /// </remarks>
        public bool CanCrowbar => DeconstructTools.Contains(PryingToolQuality);

        /// <summary>
        /// These play when the mob has shoes on.
        /// </summary>
        [DataField("footstepSounds")] public SoundSpecifier? FootstepSounds { get; private set; }

        /// <summary>
        /// These play when the mob has no shoes on.
        /// </summary>
        [DataField("barestepSounds")] public SoundSpecifier? BarestepSounds { get; private set; } = new SoundCollectionSpecifier("BarestepHard");

        [DataField("friction")] public float Friction { get; set; } = 0.2f;

        [DataField("variants")] public byte Variants { get; set; } = 1;

        /// <summary>
        /// This controls what variants the `variantize` command is allowed to use.
        /// </summary>
        [DataField("placementVariants")] public float[] PlacementVariants { get; set; } = { 1f };

        [DataField("thermalConductivity")] public float ThermalConductivity = 0.04f;

        // Heat capacity is opt-in, not opt-out.
        [DataField("heatCapacity")] public float HeatCapacity = Atmospherics.MinimumHeatCapacity;

        [DataField("itemDrop", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemDropPrototypeName { get; private set; } = "FloorTileItemSteel";

        // TODO rename data-field in yaml
        /// <summary>
        /// Whether or not the tile is exposed to the map's atmosphere.
        /// </summary>
        [DataField("isSpace")] public bool MapAtmosphere { get; private set; }

        /// <summary>
        ///     Friction override for mob mover in <see cref="SharedMoverController"/>
        /// </summary>
        [DataField("mobFriction")]
        public float? MobFriction { get; private set; }

        /// <summary>
        ///     No-input friction override for mob mover in <see cref="SharedMoverController"/>
        /// </summary>
        [DataField("mobFrictionNoInput")]
        public float? MobFrictionNoInput { get; private set; }

        /// <summary>
        ///     Accel override for mob mover in <see cref="SharedMoverController"/>
        /// </summary>
        [DataField("mobAcceleration")]
        public float? MobAcceleration { get; private set; }

        [DataField("sturdy")] public bool Sturdy { get; private set; } = true;

        /// <summary>
        /// Can weather affect this tile.
        /// </summary>
        [DataField("weather")] public bool Weather = false;

        /// <summary>
        /// Is this tile immune to RCD deconstruct.
        /// </summary>
        [DataField("indestructible")] public bool Indestructible = false;

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
