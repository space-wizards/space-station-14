using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class DoorWelded : GraphCondition, IWithInverted, IDoorStateCondition
    {

        public bool Inverted => !Welded;

        [DataField("welded")]
        public bool Welded { get; private set; } = true;

        public override bool DoExamine(ExaminedEvent args)
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

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Welded
                    ? "construction-guide-condition-door-weld"
                    : "construction-guide-condition-door-unweld",
            };
        }


        public DoorState TargetState => DoorState.Welded;
    }
}
