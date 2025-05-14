// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Replicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReplicatorComponent : Component
{
    /// <summary>
    /// If a replicator is Queen, it will spawn a nest when it spawns.
    /// </summary>
    [DataField]
    public bool Queen;

    /// <summary>
    /// Current upgrade stage.
    /// </summary>
    [DataField]
    public int UpgradeStage;

    /// <summary>
    /// Used in determining what action should be granted on leveling up the nest.
    /// </summary>
    public int TargetUpgradeStage;

    /// <summary>
    /// Used to store related replicators on a queen after the nest is destroyed, so they can be transferred to the new nest.
    /// </summary>
    public HashSet<Entity<ReplicatorComponent>> RelatedReplicators = [];

    /// <summary>
    /// Used to store the EntityUid of the source nest of this replicator.
    /// </summary>
    public EntityUid? MyNest = null;

    [DataField]
    public EntProtoId Level2Id = "MobReplicatorTier2";

    [DataField]
    public EntProtoId Level3Id = "MobReplicatorTier3";

    [DataField]
    public EntProtoId Level2Action = "ActionReplicatorUpgrade2";

    [DataField]
    public EntProtoId Level3Action = "ActionReplicatorUpgrade3";

    /// <summary>
    /// The action to spawn a new nest.
    /// </summary>
    [DataField]
    public EntProtoId SpawnNewNestAction = "ActionReplicatorSpawnNest";
    // prevent adding additional nest action if someone ghosts out and re-attaches
    public bool HasSpawnedNest;
}

[Serializable, NetSerializable]
public enum ReplicatorVisuals : byte
{
    Combat
}
