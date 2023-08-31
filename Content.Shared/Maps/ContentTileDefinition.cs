using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Maps
{
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, IInheritingPrototype, ITileDefinition
    {
        public const string SpaceID = "Space";

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ContentTileDefinition>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        [IdDataField] public string ID { get; } = string.Empty;

        public ushort TileId { get; private set; }

        [DataField("name")]
        public string Name { get; private set; } = "";
        [DataField("sprite")] public ResPath? Sprite { get; private set; }

        [DataField("edgeSprites")] public Dictionary<Direction, ResPath> EdgeSprites { get; private set; } = new();

        [DataField("edgeSpritePriority")] public int EdgeSpritePriority { get; private set; } = 0;

        [DataField("isSubfloor")] public bool IsSubFloor { get; private set; }

        [DataField("baseTurf")]
        public string BaseTurf { get; private set; } = string.Empty;

        [DataField("canCrowbar")] public bool CanCrowbar { get; private set; }

        /// <summary>
        /// Whether this tile can be pried by an advanced prying tool if not pryable otherwise.
        /// </summary>
        [DataField("canAxe")] public bool CanAxe { get; private set; }

        [DataField("canWirecutter")] public bool CanWirecutter { get; private set; }

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
        [DataField("placementVariants")] public float[] PlacementVariants { get; set; } = new [] { 1f };

        [DataField("thermalConductivity")] public float ThermalConductivity = 0.04f;

        // Heat capacity is opt-in, not opt-out.
        [DataField("heatCapacity")] public float HeatCapacity = Atmospherics.MinimumHeatCapacity;

        [DataField("itemDrop", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemDropPrototypeName { get; private set; } = "FloorTileItemSteel";

        [DataField("isSpace")] public bool IsSpace { get; private set; }

        #region Weightless behavior

        /// <summary>
        ///     Will make mobs use weightless movement on them, but not necessarily space (like ice tiles)
        /// </summary>
        [DataField("weightlessMovement")]
        public bool WeightlessMovement { get; private set; }

        /// <summary>
        ///     If we have <see cref="WeightlessMovement"/>, what should the friction be?
        /// </summary>
        [DataField("weightlessFriction")]
        public float? WeightlessFriction { get; private set; }

        /// <summary>
        ///     If we have <see cref="WeightlessMovement"/>, what should the no-input friction be?
        /// </summary>
        [DataField("weightlessFrictionNoInput")]
        public float? WeightlessFrictionNoInput { get; private set; }

        /// <summary>
        ///     If we have <see cref="WeightlessMovement"/>, what should the modifier be?
        /// </summary>
        [DataField("weightlessModifier")]
        public float? WeightlessModifier { get; private set; }

        /// <summary>
        ///     If we have <see cref="WeightlessMovement"/>, what should the acceleration be?
        /// </summary>
        [DataField("weightlessAcceleration")]
        public float? WeightlessAcceleration { get; private set; }

        #endregion

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
