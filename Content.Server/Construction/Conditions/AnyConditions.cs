using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AnyConditions : GraphCondition, IAnyCondition
    {
        IEnumerable<ICondition> IAnyCondition.Conditions => Conditions;

        [DataField("conditions")]
        public GraphCondition[] Conditions { get; private set; } = Array.Empty<GraphCondition>();

        public override bool DoExamine(ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("construction-examine-condition-any-conditions"));

            foreach (var condition in Conditions)
            {
                condition.DoExamine(args);
            }

            return true;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = "construction-guide-condition-any-conditions",
            };

            foreach (var condition in Conditions)
            {
                foreach (var entry in condition.GenerateGuideEntry())
                {
                    entry.Padding += 4;
                    yield return entry;
                }
            }
        }
    }
}
