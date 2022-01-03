using Content.Server.Hands.Components;
using Content.Server.Holiday;
using Content.Shared.Item;
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

            var entMan = IoCManager.Resolve<IEntityManager>();

            var entity = entMan.SpawnEntity(Prototype, entMan.GetComponent<TransformComponent>(mob).Coordinates);

            if (!entMan.TryGetComponent(entity, out SharedItemComponent? item) || !entMan.TryGetComponent(mob, out HandsComponent? hands))
                return;

            hands.PutInHand(item, false);
        }
    }
}
