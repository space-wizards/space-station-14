using System.Collections.Generic;
using Content.Server.Doors.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DoorWelded : IGraphCondition
    {
        [DataField("welded")]
        public bool Welded { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ServerDoorComponent? doorComponent))
                return false;

            return doorComponent.IsWeldedShut == Welded;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            if (!entity.TryGetComponent(out ServerDoorComponent? door)) return false;

            if (door.IsWeldedShut != Welded)
            {
                if (Welded == true)
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-weld", ("entityName", entity.Name)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-unweld", ("entityName", entity.Name)) + "\n");
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
