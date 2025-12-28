using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday;

/// <summary>
/// Condition that checks if the current Holiday is
/// </summary>
public sealed class IsHolidayCondition : EntitytableCondition
{
    /// <summary>
    ///  What Holiday it's checking for
    /// </summary>
    [DataField]
    public string Holiday { get; private set; } = string.Empty;

    private static HolidaySystem? _holidaySystem;

    public override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        _holidaySystem ??= IoCManager.Resolve<HolidaySystem>();

        return _holidaySystem.IsCurrentlyHoliday(Holiday); // I'd make it check for null but if there's no holiday going on it *is* correct for setting it true
    }
}
