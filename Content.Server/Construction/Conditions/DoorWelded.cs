using Content.Shared.Construction;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class DoorWelded : IGraphCondition
    {
        [DataField("welded")]
        public bool Welded { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out DoorComponent? doorComponent))
                return false;

            return doorComponent.State == DoorState.Welded;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(entity, out DoorComponent? door)) return false;

            var isWelded = door.State == DoorState.Welded;
            if (isWelded != Welded)
            {
                if (Welded)
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-weld", ("entityName", entMan.GetComponent<MetaDataComponent>(entity).EntityName)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-unweld", ("entityName", entMan.GetComponent<MetaDataComponent>(entity).EntityName)) + "\n");
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Welded
                    ? "construction-guide-condition-door-weld"
                    : "construction-guide-condition-door-unweld",
            };
        }
    }
}
