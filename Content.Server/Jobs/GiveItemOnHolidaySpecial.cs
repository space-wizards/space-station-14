using Content.Server.Holiday;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Jobs
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class GiveItemOnHolidaySpecial : JobSpecial
    {
        [DataField("holiday", customTypeSerializer:typeof(PrototypeIdSerializer<HolidayPrototype>))]
        public string Holiday { get; private set; } = string.Empty;

        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Prototype { get; private set; } = string.Empty;

        public override void AfterEquip(EntityUid mob)
        {
            if (string.IsNullOrEmpty(Holiday) || string.IsNullOrEmpty(Prototype))
                return;

            var sysMan = IoCManager.Resolve<IEntitySystemManager>();

            if (!sysMan.GetEntitySystem<HolidaySystem>().IsCurrentlyHoliday(Holiday))
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();

            var entity = entMan.SpawnEntity(Prototype, entMan.GetComponent<TransformComponent>(mob).Coordinates);

            sysMan.GetEntitySystem<SharedHandsSystem>().PickupOrDrop(mob, entity);
        }
    }
}
