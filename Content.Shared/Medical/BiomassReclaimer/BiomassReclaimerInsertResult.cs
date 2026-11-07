namespace Content.Shared.Medical.BiomassReclaimer;

/// <summary>
/// Whether an entity can be processed by a biomass reclaimer.
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
