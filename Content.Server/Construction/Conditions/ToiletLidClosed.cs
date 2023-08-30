using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Toilet;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ToiletLidClosed : IGraphCondition
    {
        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ToiletComponent? toilet))
                return false;

            return !toilet.LidOpen;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ToiletComponent? toilet)) return false;
            if (!toilet.LidOpen) return false;

            args.PushMarkup(Loc.GetString("construction-examine-condition-toilet-lid-closed") + "\n");
            return true;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = "construction-step-condition-toilet-lid-closed"
            };
        }
    }
}
