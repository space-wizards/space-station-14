using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Lock;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class Locked : GraphConditionBase<ILockedCondition>, ILockedCondition, IWithInverted
    {
        public bool Inverted => !IsLocked;

        [DataField("locked")]
        public bool IsLocked { get; private set; } = true;

        public override bool DoExamine(ExaminedEvent args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var entity = args.Examined;

            if (!entMan.TryGetComponent(entity, out LockComponent? lockcomp))
                return true;

            switch (IsLocked)
            {
                case true when !lockcomp.Locked:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-lock"));
                    return true;
                case false when lockcomp.Locked:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-unlock"));
                    return true;
            }

            return false;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = IsLocked
                    ? "construction-step-condition-wire-panel-lock"
                    : "construction-step-condition-wire-panel-unlock"
            };
        }


    }
}
