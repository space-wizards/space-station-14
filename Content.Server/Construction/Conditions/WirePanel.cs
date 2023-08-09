using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class WirePanel : IGraphCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            //if it doesn't have a wire panel, then just let it work.
            if (!entityManager.TryGetComponent(uid, out WiresComponent? wires))
                return true;

            return wires.IsPanelOpen == Open;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out WiresComponent? wires)) return false;

            switch (Open)
            {
                case true when !wires.IsPanelOpen:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-wire-panel-open"));
                    return true;
                case false when wires.IsPanelOpen:
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
