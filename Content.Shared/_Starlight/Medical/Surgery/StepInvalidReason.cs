namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public enum StepInvalidReason
{
    None,
    NeedsOperatingTable,
    Armor,
    MissingTool,
    DisabledTool,
    TooHigh,
}
