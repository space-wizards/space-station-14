using Content.Server.Power.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ApcPanel : IGraphCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ApcComponent? apc))
                return true;

            return apc.IsApcOpen == Open;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ApcComponent? apc)) return false;

            switch (Open)
            {
                case true when !apc.IsApcOpen:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-apc-open"));
                    return true;
                case false when apc.IsApcOpen:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-apc-close"));
                    return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Open
                    ? "construction-step-condition-apc-open"
                    : "construction-step-condition-apc-close"
            };
        }
    }
}
