using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MobStateCondition : EntityConditionBase<IMobStateCondition>, IMobStateCondition
{
    /// <summary>
    /// The mobstate necessary to fulfill this condition.
    /// </summary>
    [DataField]
    public MobState Mobstate { get; set; } = MobState.Alive;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-mob-state-condition", ("state", Mobstate));
}
