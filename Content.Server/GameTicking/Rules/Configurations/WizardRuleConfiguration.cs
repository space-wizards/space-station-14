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

public sealed class WizardRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "Wizard";

    [DataField("minPlayers")]
    public int MinPlayers = 15;

    /// <summary>
    ///     This INCLUDES the wizards. So a value of 3 is satisfied by 2 players & 1 wizard
    /// </summary>
    [DataField("playersPerWizard")]
    public int PlayersPerWizard = 15;

    [DataField("maxWizards")]
    public int MaxWizards = 2;

    [DataField("randomHumanoidSettings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
    public string RandomHumanoidSettingsPrototype = "WizardHumanoid";

    [DataField("spawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string SpawnPointPrototype = "SpawnPointWizard";

    [DataField("ghostSpawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string GhostSpawnPointProto = "SpawnPointGhostWizard";

    [DataField("wizardRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string WizardRoleProto = "Wizard";

    [DataField("wizardStartGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string WizardStartGearPrototype = "WizardBlueGear";

    [DataField("wizardFirstNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string WizardFirstNames = "names_wizard_first";

    [DataField("wizardLastNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string WizardLastNames = "names_wizard_last";

    [DataField("endsRound")]
    public bool EndsRound = true;

    [DataField("shuttleMap", customTypeSerializer: typeof(ResourcePathSerializer))]
    public ResourcePath? WizardShuttleMap = new("/Maps/Shuttles/wizard.yml");
}
