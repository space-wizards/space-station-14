// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

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
    /// Items containing components or tags on this list will not give any points when consumed. 
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();

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
    public int BonusPointsAlive = 1;
    /// <summary>
    /// The number of additional points given for incapacitated humanoid targets.
    /// Is multiplied by the current level.
    /// </summary>
    [DataField]
    public int BonusPointsHumanoid = 2;
    /// <summary>
    /// The number of points required to spawn a new replicator. 
    /// Does not increase over time.
    /// </summary>
    [DataField]
    public int SpawnNewAt = 20;
    /// <summary>
    /// The number of points required to upgrade existing replicators & the nest itself.
    /// Multiplied by current nest level.
    /// </summary>
    [DataField]
    public int UpgradeAt = 40;

    /// <summary>
    /// The level at which the nest stops growing. It will still produce and upgrade replicators, 
    /// but the upgrade thresholds will not increase, and the nest will start ignoring weightlessness.
    /// </summary>
    [DataField]
    public int EndgameLevel = 3;

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
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
    public HashSet<EntityUid> SpawnedMinions = [];
    public HashSet<EntityUid> UnclaimedSpawners = [];
    public int NextSpawnAt;
    public int NextUpgradeAt;
    [DataField, AutoNetworkedField]
    public bool NeedsUpdate;
}

[Serializable, NetSerializable]
public enum ReplicatorNestVisuals : byte
{
    Level1,
    Level2,
    Level3
}

[Serializable, NetSerializable]
public sealed partial class ReplicatorNestSizeChangedEvent : EntityEventArgs
{

}
