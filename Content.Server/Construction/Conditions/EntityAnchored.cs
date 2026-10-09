using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class EntityAnchored : GraphCondition, IEntityAnchoredCondition, IWithInverted
    {

        public bool Inverted => !Anchored;

        [DataField("anchored")] public bool Anchored { get; private set; } = true;

        public override bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            var anchored = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).Anchored;

            switch (Anchored)
            {
                case true when !anchored:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-entity-anchored"));
                    return true;
                case false when anchored:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-entity-unanchored"));
                    return true;
            }

            return false;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Anchored
                    ? "construction-step-condition-entity-anchored"
                    : "construction-step-condition-entity-unanchored",
                Icon = new SpriteSpecifier.Rsi(new ("Objects/Tools/wrench.rsi"), "icon"),
            };
        }


    }
}
