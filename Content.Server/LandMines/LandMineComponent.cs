namespace Content.Server.LandMines;

[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// Defines whether landmine will explode when stepping on it or when stepping off it
    /// True - StepTrigger, False - StepOffTrigger
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ExplodeImmediately = false;

    /// <summary>
    /// Distance at which landmine beep sound can be heard by players
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly float TriggerAudioRange = 10f;

    /// <summary>
    /// Landmine beep SFX path in Resources folder
    /// </summary>
    public readonly string MineBeepAudioPath = "/Audio/Effects/beep_landmine.ogg";
}
