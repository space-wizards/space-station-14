using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Holiday;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
///     Condition that passes only when a certain holiday is active.
/// </summary>
public sealed partial class IsHolidayCondition : EntityTableCondition
{
    /// <summary>
    ///     The holiday to check.
    /// </summary>
    [DataField]
    public ProtoId<HolidayPrototype> Holiday;

    private static SharedHolidaySystem? _holiday;

    protected override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        // Don't resolve this repeatedly
        _holiday ??= entMan.System<SharedHolidaySystem>();

        return _holiday.IsCurrentlyHoliday(Holiday);
    }
}
