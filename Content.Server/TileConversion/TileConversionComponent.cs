using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.TileConversion;

[RegisterComponent, Access(typeof(TileConversionSystem))]
[AutoGenerateComponentPause]
public sealed partial class TileConversionComponent : Component
{
    /// <summary>
    /// Our timer for conversion checks.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField] public TimeSpan ConversionTimer;

    /// <summary>
    /// the list of tiles that can be converted by this source.
    /// </summary>
    [DataField]
    public HashSet<Vector2i> ConvertableTiles = [];

    /// <summary>
    /// If this conversion source can move. if true, only convert the immediate area around it.
    /// Slightly hacky but works for our purposes.
    /// </summary>
    [DataField]
    public bool Mobile;

    /// <summary>
    /// if this conversion source should floodfill through all convertable tiles to initialize its convertable tile set on activation.
    /// </summary>
    [DataField]
    public bool FloodFillStarting;

    /// <summary>
    /// How many times has this conversion source ticked?
    /// </summary>
    [DataField]
    public int ConversionTicks;

    /// <summary>
    /// The maximum amount of ticks this source can do.
    /// </summary>
    [DataField]
    public int ConversionMaxTicks = 50;

    /// <summary>
    /// The chance that a tile and/or wall is replaced.
    /// </summary>
    [DataField]
    public float ConversionChance = 0.51f;

    /// <summary>
    /// The reduction applied to conversion chance every tick.
    /// </summary>
    [DataField]
    public float ChanceReduction;

    /// <summary>
    /// Wether or not the TileConversionSystem should be running on this entity. use TileConversionSystem.Enable() instead of directly interacting with this variable.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Wether or not the TileConversionSystem should spawn VFX when converting tiles and walls.
    /// </summary>
    [DataField]
    public bool UseVfx = true;

    /// <summary>
    /// Wether or not the TileConversionSystem should ignore this component when it reaches max growth. Saves performance.
    /// </summary>
    [DataField]
    public bool AutoDisable = true;

    /// <summary>
    /// How much time between tile conversions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ConversionTime = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The tile we spawn when replacing a normal tile.
    /// </summary>
    [DataField] //not a dict like the entity conversion below because there's too many fucking tiles
    public List<ProtoId<ContentTileDefinition>> ConversionTiles =
    [
        "FloorCosmicCorruption",
    ];

    /// <summary>
    /// Dictionary for what entities to convert to which prototypes.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> EntityConversionDict = new()
    {
        // Walls
        {"WallWood", "WallMalign"},
        {"WallSolid", "WallMalign"},
        {"WallReinforced", "WallMalign"},
        {"WallShuttle", "WallMalign"},

        // Windows
        {"ShuttleWindow", "MalignWindow"},
        {"Window", "MalignWindow"},
        {"ReinforcedWindow", "MalignWindow"},
        {"PlasmaWindow", "MalignWindow"},
        {"ReinforcedPlasmaWindow", "MalignWindow"},
        {"UraniumWindow", "MalignWindow"},
        {"ReinforcedUraniumWindow", "MalignWindow"},
    };

    /// <summary>
    /// The VFX entity we spawn when corruption occurs.
    /// </summary>
    [DataField]
    public EntProtoId TileConvertVfx = "EffectCosmicTileSpawn";

}
