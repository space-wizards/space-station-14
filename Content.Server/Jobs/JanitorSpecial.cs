#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Holiday.Interfaces;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Jobs
{
    [UsedImplicitly]
    public class JanitorSpecial : JobSpecial
    {
        private string _holiday = string.Empty;
        private string _prototype = string.Empty;

        protected override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _holiday, "holiday", string.Empty);
            serializer.DataField(ref _prototype, "prototype", string.Empty);
        }

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
