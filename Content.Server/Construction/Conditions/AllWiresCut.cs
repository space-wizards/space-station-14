using Content.Server.Wires;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.EntityConditions;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     A condition that requires all wires to be cut (or intact)
    ///     Returns true if the entity doesn't have a wires component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AllWiresCut : GraphCondition
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public override bool DoExamine(ExaminedEvent args)
        {
            if (IoCManager.Resolve<SharedConditionEvaluationSystem>()
                .IsConditionSatisfied(this, args.Examined, args.Examiner))
                return false;

            args.PushMarkup(Loc.GetString(Value
                ? "construction-examine-condition-all-wires-cut"
                : "construction-examine-condition-all-wires-intact"));
            return true;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Value
                    ? "construction-guide-condition-all-wires-cut"
                    : "construction-guide-condition-all-wires-intact"
            };
        }
    }
}
