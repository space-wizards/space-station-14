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

    public float TriggerAudioRange = 10f;
}
