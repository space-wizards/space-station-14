namespace Content.Shared.Medical.BiomassReclaimer;

/// <summary>
/// Result of validating a target for insertion into a biomass reclaimer.
/// </summary>
public enum BiomassReclaimerInsertResult : byte
{
    Success,
    InvalidTarget,
    Unanchored,
    Unpowered,
    TargetAlive,
    Busy,
    SoulPresent
}
