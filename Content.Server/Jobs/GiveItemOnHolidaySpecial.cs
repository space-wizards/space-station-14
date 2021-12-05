using Content.Server.Hands.Components;
using Content.Server.Holiday;
using Content.Server.Items;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Jobs
{
    [UsedImplicitly]
    [DataDefinition]
    public class GiveItemOnHolidaySpecial : JobSpecial
    {
        [DataField("holiday", customTypeSerializer:typeof(PrototypeIdSerializer<HolidayPrototype>))]
        public string Holiday { get; } = string.Empty;

        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Prototype { get; } = string.Empty;

        public override void AfterEquip(EntityUid mob)
        {
            if (string.IsNullOrEmpty(Holiday) || string.IsNullOrEmpty(Prototype))
                return;

            if (!EntitySystem.Get<HolidaySystem>().IsCurrentlyHoliday(Holiday))
                return;

            var entity = IoCManager.Resolve<IEntityManager>().SpawnEntity(Prototype, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mob).Coordinates);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ItemComponent? item) || !IoCManager.Resolve<IEntityManager>().TryGetComponent(mob, out HandsComponent? hands))
                return;

            hands.PutInHand(item, false);
        }
    }
}
