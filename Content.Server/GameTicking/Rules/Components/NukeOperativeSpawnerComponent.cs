namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for tagging a spawn point as a nuke operative spawn point
/// and providing loadout + name for the operative on spawn.
/// </summary>
[RegisterComponent]
public sealed class NukeOperativeSpawnerComponent : Component
{
    [DataField("name")]
    public string OperativeName = "";

    [DataField("rolePrototype")]
    public string OperativeRolePrototype = "";

    [DataField("startingGearPrototype")]
    public string OperativeStartingGear = "";
}
