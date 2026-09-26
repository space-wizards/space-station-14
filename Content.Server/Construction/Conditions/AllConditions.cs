using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AllConditions : GraphConditionBase<IAllCondition>,IAllCondition
    {
        IEnumerable<ICondition> IAllCondition.Conditions => Conditions;

        [DataField("conditions")]
        public GraphCondition[] Conditions { get; private set; } = Array.Empty<GraphCondition>();

        public override bool DoExamine(ExaminedEvent args)
        {
            var ret = false;

            foreach (var condition in Conditions)
            {
                ret |= condition.DoExamine(args);
            }

            return ret;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            foreach (var condition in Conditions)
            {
                foreach (var entry in condition.GenerateGuideEntry())
                {
                    yield return entry;
                }
            }
        }

    }
}
