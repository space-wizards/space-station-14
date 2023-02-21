namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for tagging a spawn point as a wizard spawn point
/// and providing loadout + name for the wizard on spawn.
/// TODO: Remove once systems can request spawns from the ghost role system directly.
/// </summary>
[RegisterComponent]
[Access(typeof(WizardRuleSystem))]
public sealed class WizardSpawnerComponent : Component
{
    [DataField("name")]
    public string WizardName = "";

    [DataField("rolePrototype")]
    public string WizardRolePrototype = "";

    [DataField("startingGearPrototype")]
    public string WizardStartingGear = "";
}
