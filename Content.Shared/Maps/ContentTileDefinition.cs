using Content.Shared.Atmos;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Maps;

[Prototype("tile")]
public sealed partial class ContentTileDefinition : IPrototype, IInheritingPrototype, ITileDefinition
{
    public static readonly ProtoId<ToolQualityPrototype> PryingToolQuality = "Prying";

    public const string SpaceID = "Space";

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ContentTileDefinition>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [IdDataField] public string ID { get; private set; } = string.Empty;

    public ushort TileId { get; private set; }

    [DataField]
    public string Name { get; private set; } = "";
    [DataField] public ResPath? Sprite { get; private set; }

    [DataField] public Dictionary<Direction, ResPath> EdgeSprites { get; private set; } = new();

    [DataField] public int EdgeSpritePriority { get; private set; } = 0;

    [DataField("isSubfloor")] public bool IsSubFloor { get; private set; }

    [DataField]
    public ProtoId<ContentTileDefinition>? BaseTurf { get; private set; }

    /// <summary>
    /// On what tiles this tile can be placed on. BaseTurf is already included.
    /// </summary>
    [DataField]
    public List<ProtoId<ContentTileDefinition>> BaseWhitelist { get; private set; } = new();

    [DataField]
    public PrototypeFlags<ToolQualityPrototype> DeconstructTools { get; set; } = new();

    /// <summary>
    /// Effective mass of this tile for grid impacts.
    /// </summary>
    [DataField]
    public float Mass = SharedShuttleSystem.TileDensityMultiplier;

    /// <remarks>
    /// Legacy AF but nice to have.
    /// </remarks>
    public bool CanCrowbar => DeconstructTools.Contains(PryingToolQuality);

    /// <summary>
    /// These play when the mob has shoes on.
    /// </summary>
    [DataField] public SoundSpecifier? FootstepSounds { get; private set; }

    /// <summary>
    /// These play when the mob has no shoes on.
    /// </summary>
    [DataField] public SoundSpecifier? BarestepSounds { get; private set; } = new SoundCollectionSpecifier("BarestepHard");

    /// <summary>
    /// Base friction modifier for this tile.
    /// </summary>
    [DataField] public float Friction { get; set; } = 1f;

    [DataField] public byte Variants { get; set; } = 1;

    /// <summary>
    /// Allows the tile to be rotated/mirrored when placed on a grid.
    /// </summary>
    [DataField] public bool AllowRotationMirror { get; set; } = false;

    /// <summary>
    /// This controls what variants the `variantize` command is allowed to use.
    /// If null, the distribution will be uniform.
    /// </summary>
    [DataField] public float[]? PlacementVariants { get; set; } = null;

    [DataField] public float ThermalConductivity = 0.04f;

    // Heat capacity is opt-in, not opt-out.
    [DataField] public float HeatCapacity = Atmospherics.MinimumHeatCapacity;

    [DataField("itemDrop")]
    public EntProtoId? ItemDropPrototypeName { get; private set; } = "FloorTileItemSteel";

    // TODO rename data-field in yaml
    /// <summary>
    /// Whether or not the tile is exposed to the map's atmosphere.
    /// </summary>
    [DataField("isSpace")] public bool MapAtmosphere { get; private set; }

    /// <summary>
    /// Friction override for mob mover in <see cref="SharedMoverController"/>
    /// </summary>
    [DataField]
    public float? MobFriction { get; private set; }

    /// <summary>
    /// Accel override for mob mover in <see cref="SharedMoverController"/>
    /// </summary>
    [DataField]
    public float? MobAcceleration { get; private set; }

    [DataField] public bool Sturdy { get; private set; } = true;

    /// <summary>
    /// Can weather affect this tile.
    /// </summary>
    [DataField] public bool Weather = false;

    /// <summary>
    /// Is this tile immune to RCD deconstruct.
    /// </summary>
    [DataField] public bool Indestructible = false;

    /// <summary>
    /// Hide this tile in the tile placement editor.
    /// </summary>
    [DataField] public bool EditorHidden { get; private set; } = false;

    public void AssignTileId(ushort id)
    {
        TileId = id;
    }
}
