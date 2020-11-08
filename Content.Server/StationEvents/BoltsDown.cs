#nullable enable
using JetBrains.Annotations;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class BoltsDown : StationEvent
    {
        public override string Name => "BoltsDown";
        public override StationEventWeight Weight => StationEventWeight.Low;
        public override int? MaxOccurrences => 1;
        public override string StartAnnouncement => Loc.GetString(
            "The clover hat hackers turned the bolts of all the airlocks in the station down. We have dispatched high quality hacking equipment at every crewmember location so that this productive shift can continue");
        protected override string EndAnnouncement => Loc.GetString(
            "Our cybersecurity team has dealt with the problem and restarted all the airlocks bolts in the station. Have a nice shift.");
        protected override string EndAudio => "/Audio/Effects/alert.ogg";

        public override void Setup()
        {
            base.Setup();
            EndWhen = IoCManager.Resolve<IRobustRandom>().Next(120, 180);
        }

        public override void Start()
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();
            foreach (var component in componentManager.EntityQuery<AirlockComponent>()) component.BoltsDown = true;

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            foreach (var player in playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity == null || !player.AttachedEntity.TryGetComponent(out InventoryComponent? inventory)) return;
                if (inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.BELT, out ItemComponent? item)
                    && item?.Owner.Prototype?.ID == "UtilityBeltClothingFilledEvent") return;
                if (player.AttachedEntity.TryGetComponent<IDamageableComponent>(out var damageable)
                && damageable.CurrentState == DamageState.Dead) return;

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var playerPos = player.AttachedEntity.Transform.Coordinates;
                entityManager.SpawnEntity("UtilityBeltClothingFilledEvent", playerPos);
            }
        }
        public override void End()
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();
            foreach (var component in componentManager.EntityQuery<AirlockComponent>()) component.BoltsDown = false;
            base.End();
        }
    }
}
