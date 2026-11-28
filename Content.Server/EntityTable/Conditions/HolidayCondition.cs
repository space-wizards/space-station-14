using Content.Server.Holiday;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityTable.Conditions;

/// <summary>
/// Condition that checks whether its a given holiday.
/// </summary>
public sealed partial class HolidayCondition : EntityTableCondition
{
    /// <summary>
    /// Maximum amount of time the round could go on for this condition to pass.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<HolidayPrototype> Holiday;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return entMan.System<HolidaySystem>().IsCurrentlyHoliday(Holiday);
    }
}
