using Content.Server._Impstation.CosmicCult.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
[Access(typeof(CosmicCorruptingSystem))]
public sealed partial class CosmicCorruptingComponent : Component
{
    /// <summary>
    /// Our timer for corruption checks.
    /// </summary>
    [ViewVariables]
    [AutoPausedField] public TimeSpan CorruptionTimer = default!;

    /// <summary>
    /// the list of tiles that can be corrupted by this corruptor.
    /// </summary>
    [DataField]
    public HashSet<Vector2i> CorruptableTiles = [];

    /// <summary>
    /// If this corruption source can move. if true, only corrupt the immediate area around it.
    /// Slightly hacky but works for our purposes.
    /// </summary>
    [DataField]
    public bool Mobile = false;

    /// <summary>
    /// if this corruption source should floodfill through all corruptible tiles to initialise its corruptible tile set on activation.
    /// </summary>
    [DataField]
    public bool FloodFillStarting = false;

    /// <summary>
    /// How many times has this corruption source ticked?
    /// </summary>
    [DataField]
    public int CorruptionTicks = 0;

    /// <summary>
    /// The maximum amount of ticks this source can do.
    /// </summary>
    [DataField]
    public int CorruptionMaxTicks = 50;

    /// <summary>
    /// The chance that a tile and/or wall is replaced.
    /// </summary>
    [DataField]
    public float CorruptionChance = 0.51f;

    /// <summary>
    /// The reduction applied to corruption chance every tick.
    /// </summary>
    [DataField]
    public float CorruptionReduction = 0f;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should be running on this entity. use CosmicCorruptingSystem.Enable() instead of directly interacting with this variable.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should spawn VFX when converting tiles and walls.
    /// </summary>
    [DataField]
    public bool UseVFX = true;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should ignore this component when it reaches max growth. Saves performance.
    /// </summary>
    [DataField]
    public bool AutoDisable = true;

    /// <summary>
    /// How much time between tile corruptions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CorruptionSpeed = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The tile we spawn when replacing a normal tile.
    /// </summary>
    [DataField]
    public ProtoId<ContentTileDefinition> ConversionTile = "FloorCosmicCorruption";

    /// <summary>
    /// The wall we spawn when replacing a normal wall.
    /// </summary>
    [DataField]
    public EntProtoId ConversionWall = "WallCosmicCult";

    /// <summary>
    /// the door we spawn when replacing a secret door
    /// </summary>
    [DataField]
    public EntProtoId ConversionDoor = "DoorCosmicCult";

    /// <summary>
    /// The VFX entity we spawn when corruption occurs.
    /// </summary>
    [DataField]
    public EntProtoId TileConvertVFX = "CosmicFloorSpawnVFX";

    /// <summary>
    /// The VFX entity we spawn when walls get deleted.
    /// </summary>
    [DataField]
    public EntProtoId TileDisintegrateVFX = "CosmicGenericVFX";

}
