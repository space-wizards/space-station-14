namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for...
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
