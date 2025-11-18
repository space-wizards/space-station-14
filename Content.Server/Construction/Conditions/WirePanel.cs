using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class WirePanel : IGraphCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            //if it doesn't have a wire panel, then just let it work.
            if (!entityManager.TryGetComponent<WiresPanelComponent>(uid, out var wires))
                return true;

            return wires.Open == Open;
        }

        public bool DoExamine(ExaminedEvent args)
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

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
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
