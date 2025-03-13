using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicCorruptingComponent : Component
{
    /// <summary>
    /// Our timer for corruption checks.
    /// </summary>
    [ViewVariables]
    [AutoPausedField] public TimeSpan CorruptionTimer = default!;

    /// <summary>
    /// The starting radius of the effect.
    /// </summary>
    [DataField] public float CorruptionRadius = 2;

    /// <summary>
    /// The maximum radius the corruption effect can grow to.
    /// </summary>
    [DataField] public float CorruptionMaxRadius = 50;

    /// <summary>
    /// The chance that a tile and/or wall is replaced.
    /// </summary>
    [DataField] public float CorruptionChance = 0.51f;

    /// <summary>
    /// The reduction applied to corruption chance every tick.
    /// </summary>
    [DataField] public float CorruptionReduction = 0f;

    /// <summary>
    /// Enables or disables the growth of the corruption radius.
    /// </summary>
    [DataField] public bool CorruptionGrowth = false;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should be running on this entity.
    /// </summary>
    [DataField] public bool Enabled = true;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should spawn VFX when converting tiles and walls.
    /// </summary>
    [DataField] public bool UseVFX = true;

    /// <summary>
    /// Wether or not the CosmicCorruptingSystem should ignore this component when it reaches max growth. Saves performance.
    /// </summary>
    [DataField] public bool AutoDisable = true;

    /// <summary>
    /// Wether or not the item should have a chance to disintegrate walls. Used for the Monument.
    /// </summary>
    [DataField] public bool Disintegrate = false;

    /// <summary>
    /// How much time between tile corruptions.
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan CorruptionSpeed = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The tile we spawn when replacing a normal tile.
    /// </summary>
    [DataField] public ProtoId<ContentTileDefinition> ConversionTile = "FloorCosmicCorruption";

    /// <summary>
    /// The wall we spawn when replacing a normal wall.
    /// </summary>
    [DataField] public EntProtoId ConversionWall = "WallCosmicCult";

    /// <summary>
    /// The VFX entity we spawn when corruption occurs.
    /// </summary>
    [DataField] public EntProtoId TileConvertVFX = "CosmicFloorSpawnVFX";

    /// <summary>
    /// The VFX entity we spawn when walls get deleted.
    /// </summary>
    [DataField] public EntProtoId TileDisintegrateVFX = "CosmicGenericVFX";

}
