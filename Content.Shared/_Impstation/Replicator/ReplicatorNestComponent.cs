// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Replicator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReplicatorNestComponent : Component
{
    /// <summary>
    /// maximum upgrade stage for *replicators,* not nests. changing this requires changing a bunch of other shit so dont mess with it
    /// </summary>
    public readonly int MaxUpgradeStage = 2;

    /// <summary>
    /// The container we're storing things in. If the nest is destroyed, anything in this will be dumped out.
    /// </summary>
    public Container Hole = default!;

    /// <summary>
    /// Items containing components or tags on this list will be rejected by the nest.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();

    /// <summary>
    /// Items containing components or tags on this list will NOT be deleted upon entering the nest, instead being stored until it's destroyed.
    /// </summary>
    [DataField]
    public EntityWhitelist PreservationWhitelist = new();

    /// <summary>
    /// Items containing components or tags on this list will be deleted upon entering the nest, regardless of whether or not they pass the whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist PreservationBlacklist = new();

    /// <summary>
    /// Total stored points. Points are acquired by putting things in the hole.
    /// is a datafield so admins can VV it
    /// </summary>
    [DataField(readOnly: true)]
    public int TotalPoints;
    /// <summary>
    /// A separate point total for when a new replicator should be spawned, so we can be more granular about balance.
    /// </summary>
    [DataField(readOnly: true)]
    public int SpawningProgress;
    /// <summary>
    /// The current level of the nest.
    /// </summary>
    [DataField(readOnly: true), AutoNetworkedField]
    public int CurrentLevel = 1;

    /// <summary>
    /// The number of additional points given for living targets.
    /// </summary>
    [DataField]
    public int BonusPointsAlive = 10;
    /// <summary>
    /// The number of additional points given for incapacitated humanoid targets.
    /// Is multiplied by the current level.
    /// </summary>
    [DataField]
    public int BonusPointsHumanoid = 0; // currently trying out setting this to 0, to discourage violence outside of self defense
    /// <summary>
    /// The number of points required to convert a tile.
    /// Does not increase.
    /// </summary>
    [DataField]
    public int TileConvertAt = 100;
    /// <summary>
    /// The number of points required to spawn a new replicator.
    /// Increases linearly with the number of unclaimed ghostroles.
    /// </summary>
    [DataField]
    public int SpawnNewAt = 300;
    /// <summary>
    /// The number of points required to upgrade existing replicators & the nest itself.
    /// Multiplied by current nest level.
    /// </summary>
    [DataField]
    public int UpgradeAt = 400;

    /// <summary>
    /// The level at which the nest stops growing. It will still produce and upgrade replicators,
    /// but the upgrade thresholds will not increase.
    /// </summary>
    [DataField]
    public int EndgameLevel = 3;

    [DataField]
    public int AnnounceAtLevel = 5;

    [DataField]
    public LocId Announcement = "replicator-level-warning";
    public bool HasAnnounced;

    [DataField]
    public float TileConversionChance = 0.05f;

    /// <summary>
    /// radius around the nest to convert tiles. increases linearly by TileConversionIncrease with each level after endgame.
    /// an extremely successful nest might hit level 12 by end of round, giving a final tile conversion radius of ~24 tiles.
    /// </summary>
    [DataField]
    public float TileConversionRadius = 1f;
    [DataField]
    public float TileConversionIncrease = 1f;

    /// <summary>
    /// Entity to be spawned when reaching spawn point thresholds.
    /// </summary>
    [DataField]
    public EntProtoId ToSpawn = "SpawnPointGhostReplicator";

    /// <summary>
    /// The action to spawn a new nest.
    /// </summary>
    [DataField]
    public EntProtoId SpawnNewNestAction = "ActionReplicatorSpawnNest";

    [DataField]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/_Impstation/Effects/falling.ogg");
    [DataField]
    public SoundSpecifier LevelUpSound = new SoundPathSpecifier("/Audio/_Impstation/Ambience/hole_2.ogg");
    [DataField]
    public SoundSpecifier UpgradeSound = new SoundPathSpecifier("/Audio/_Impstation/Misc/replicator_sfx2.ogg");
    [DataField]
    public SoundSpecifier TilePlaceSound = new SoundPathSpecifier("/Audio/_Impstation/Misc/replicator_sfx1.ogg");
    [DataField]
    public ProtoId<ContentTileDefinition> ConversionTile = "FloorReplicator";
    [DataField]
    public EntProtoId TileConversionVfx = "ReplicatorFloorSpawnVFX";

    public HashSet<EntityUid> SpawnedMinions = [];
    public HashSet<EntityUid> UnclaimedSpawners = [];
    public int NextSpawnAt;
    public int NextUpgradeAt;
    public int NextTileConvertAt;
    [DataField, AutoNetworkedField]
    public bool NeedsUpdate;
    public EntityUid PointsStorage;
}

[Serializable, NetSerializable]
public enum ReplicatorNestVisuals : byte
{
    Level1,
    Level2,
    Level3,
    Level1Unshaded,
    Level2Unshaded,
    Level3Unshaded
}

[Serializable, NetSerializable]
public sealed partial class ReplicatorNestSizeChangedEvent : EntityEventArgs
{

}
