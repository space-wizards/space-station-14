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
    public int MinPlayers = 0; //15

    /// <summary>
    ///     This INCLUDES the operatives. So a value of 3 is satisfied by 2 players & 1 operative
    /// </summary>
    [DataField("playersPerOperative")]
    public int PlayersPerOperative = 1; //5

    [DataField("maxOps")]
    public int MaxOperatives = 5;
    
    /// <summary>
    ///     This amount of TC will be distributed. Note that it distributed for all uplinks not each one
    /// </summary>
    [DataField("warTCAmount")]
    public int WarTCAmount = 280;

    /// <summary>
    ///     Amount of time given to get extra TC for war declare. 
    /// </summary>
    [DataField("warTimeLimit")]
    public TimeSpan WarTimeLimit = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Minimal crew size for war extra TC
    /// </summary>
    [DataField("warMinCrewSize")]
    public int WarMinCrewSize = 0;

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

    [DataField("outpostMap", customTypeSerializer: typeof(ResourcePathSerializer))]
    public ResourcePath? NukieOutpostMap = new("/Maps/nukieplanet.yml");

    [DataField("shuttleMap", customTypeSerializer: typeof(ResourcePathSerializer))]
    public ResourcePath? NukieShuttleMap = new("/Maps/infiltrator.yml");

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetSound = new SoundPathSpecifier("/Audio/Misc/nukeops.ogg");
}
