using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.WireHacking;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class WirePanel : IGraphCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out WiresComponent? wires))
                return false;

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
