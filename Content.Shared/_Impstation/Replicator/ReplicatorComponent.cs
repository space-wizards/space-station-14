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
    /// The duration for which a replicator of this type will be stunned upon recieving an EMP effect.
    /// </summary>
    [DataField]
    public TimeSpan EmpStunTime = TimeSpan.FromSeconds(5);

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
    /// Used to store related replicators on a queen after the nest is destroyed, so they can be transferred to the new nest.
    /// </summary>
    public HashSet<Entity<ReplicatorComponent>> RelatedReplicators = [];

    /// <summary>
    /// Used to store the EntityUid of the source nest of this replicator.
    /// </summary>
    public EntityUid? MyNest = null;

    /// <summary>
    /// actions granted when this replicator is ready to upgrade
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> UpgradeActions = [];

    /// <summary>
    /// locid for the message that gets displayed when a replicator is ready to upgrade. -self and -others are automatically appended to it when relevant
    /// this is a string because this exact locid doesn't actually exist.
    /// </summary>
    [DataField]
    public string ReadyToUpgradeMessage = "replicator-upgrade-t1";

    /// <summary>
    /// The action to spawn a new nest.
    /// </summary>
    [DataField]
    public EntProtoId SpawnNewNestAction = "ActionReplicatorSpawnNest";
    // prevent adding additional nest action if someone ghosts out and re-attaches

    public HashSet<EntityUid?> Actions = [];

    public bool HasSpawnedNest;
    // note: this is never explicitly set to false - but it defaults to false when upgrading, because the upgraded entity is a new ent.
    public bool HasBeenGivenUpgradeActions;

    [DataField]
    public LocId QueenDiedMessage = "replicator-queen-died-msg";

    /// <summary>
    /// The default first stage to revert Replicators to in the event of nest destruction.
    /// </summary>
    [DataField]
    public EntProtoId FirstStage = "MobReplicator";

    /// <summary>
    /// The default final stage to upgrade the Queen to in the event of nest destruction.
    /// </summary>
    [DataField]
    public EntProtoId FinalStage = "MobReplicatorTier3";
}

[Serializable, NetSerializable]
public enum ReplicatorVisuals : byte
{
    Combat
}
