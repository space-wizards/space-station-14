using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class WirePanel : GraphCondition, IWirePanelStateCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public override bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<WiresPanelComponent>(entity, out var panel)) return false;

            switch (Open)
            {
                case true when !panel.Open:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-wire-panel-open"));
                    return true;
                case false when panel.Open:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-wire-panel-close"));
                    return true;
            }

            return false;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Open
                    ? "construction-step-condition-wire-panel-open"
                    : "construction-step-condition-wire-panel-close"
            };
        }
    }
}
