#nullable enable
using Content.Server.Hands.Components;
using Content.Server.Holiday.Interfaces;
using Content.Server.Items;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Jobs
{
    [UsedImplicitly]
    [DataDefinition]
    public class JanitorSpecial : JobSpecial
    {
        [DataField("holiday")] private readonly string _holiday = string.Empty;
        [DataField("prototype")] private readonly string _prototype = string.Empty;

        public override void AfterEquip(IEntity mob)
        {
            base.AfterEquip(mob);

            if (string.IsNullOrEmpty(_holiday) || string.IsNullOrEmpty(_prototype)) return;
            if (!IoCManager.Resolve<IHolidayManager>().IsCurrentlyHoliday(_holiday)) return;

            var item = mob.EntityManager.SpawnEntity(_prototype, mob.Transform.Coordinates);
            if (!item.TryGetComponent(out ItemComponent? itemComp)) return;
            if (!mob.TryGetComponent(out HandsComponent? handsComponent)) return;
            handsComponent.PutInHand(itemComp, false);
        }
    }
}
