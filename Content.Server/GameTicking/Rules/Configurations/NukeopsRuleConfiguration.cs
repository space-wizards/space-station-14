using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules.Configurations;

public sealed class NukeopsRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "Nukeops";

    [DataField("minPlayers")]
    public int MinPlayers = 15;

    /// <summary>
    ///     This INCLUDES the operatives. So a value of 3 is satisfied by 2 players & 1 operative
    /// </summary>
    [DataField("playersPerOperative")]
    public int PlayersPerOperative = 5;

    [DataField("maxOps")]
    public int MaxOperatives = 5;

    /// <summary>
    /// Whether or not all of the nuclear operatives dying will end the round. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField("endsRound")]
    public bool EndsRound = true;

    /// <summary>
    /// Whether or not to spawn the nuclear operative outpost. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField("spawnOutpost")]
    public bool SpawnOutpost = true;

    /// <summary>
    /// Whether or not loneops can spawn. Set to false if a normal nukeops round is occurring.
    /// </summary>
    [DataField("canLoneOpsSpawn")]
    public bool CanLoneOpsSpawn = true;

    [DataField("randomHumanoidSettings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
    public string RandomHumanoidSettingsPrototype = "NukeOp";

    [DataField("spawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string SpawnPointPrototype = "SpawnPointNukies";

    [DataField("ghostSpawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string GhostSpawnPointProto = "SpawnPointGhostNukeOperative";

    [DataField("commanderRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string CommanderRolePrototype = "NukeopsCommander";

    [DataField("operativeRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string OperativeRoleProto = "Nukeops";

    [DataField("commanderStartingGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string CommanderStartGearPrototype = "SyndicateCommanderGearFull";

    [DataField("medicStartGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string MedicStartGearPrototype = "SyndicateOperativeMedicFull";

    [DataField("operativeStartGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string OperativeStartGearPrototype = "SyndicateOperativeGearFull";

    [DataField("eliteNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string EliteNames = "SyndicateNamesElite";

    [DataField("normalNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NormalNames = "SyndicateNamesNormal";

    [DataField("outpostMap", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath NukieOutpostMap = new("/Maps/nukieplanet.yml");

    [DataField("shuttleMap", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath NukieShuttleMap = new("/Maps/infiltrator.yml");

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetSound = new SoundPathSpecifier("/Audio/Misc/nukeops.ogg");
}
