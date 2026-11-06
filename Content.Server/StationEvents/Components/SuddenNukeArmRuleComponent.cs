namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class SuddenNukeArmRuleComponent : Component
{
    /// <summary>
    /// The nuke picked for arming.
    /// </summary>
    [DataField]
    public EntityUid? PickedNuke;

    /// <summary>
    /// The nuke that exploded.
    /// </summary>
    [DataField]
    public EntityUid? ExplodedNuke;
}
