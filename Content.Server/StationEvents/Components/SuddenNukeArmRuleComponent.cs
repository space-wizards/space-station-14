namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class SuddenNukeArmRuleComponent : Component
{
    /// <summary>
    /// The nuke that will be picked for arming.
    /// </summary>
    [DataField]
    public EntityUid? PickedNuke;
}
