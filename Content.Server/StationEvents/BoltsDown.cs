#nullable enable
using JetBrains.Annotations;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
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
        private float _elapsedTime;
        private int _eventDuration;
        protected override string StartAnnouncement => Loc.GetString(
            "The clover hat hackers turned the bolts of all the airlocks in the station down. We have dispatched high quality hacking equipment at every crewmember location so that this productive shift can continue");
        protected override string EndAnnouncement => Loc.GetString(
            "Our cybersecurity team has dealt with the problem and restarted all the airlocks bolts in the station. Have a nice shift.");
        public override void Startup()
        {
            base.Startup();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Effects/alert.ogg", AudioParams.Default.WithVolume(-10f));
            _eventDuration = IoCManager.Resolve<IRobustRandom>().Next(120, 180);

            var componentManager = IoCManager.Resolve<IComponentManager>();
            foreach (var component in componentManager.EntityQuery<AirlockComponent>()) component.BoltsDown = true;

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            foreach (var player in playerManager.GetAllPlayers())
            {
                var playerEntity = player.AttachedEntity;
                if (playerEntity == null || !playerEntity.TryGetComponent(out InventoryComponent? inventory)) return;
                if (inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.BELT, out ItemComponent? item)
                    && item?.Owner.Prototype?.ID == "UtilityBeltClothingFilledEvent") return;
                if (playerEntity.TryGetComponent(out IDamageableComponent? damageable) &&
                    playerEntity.TryGetComponent(out IMobStateComponent? mobState) &&
                    mobState.IsDead())
                {
                    return;
                }

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var playerPos = playerEntity.Transform.Coordinates;
                entityManager.SpawnEntity("UtilityBeltClothingFilledEvent", playerPos);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Effects/alert.ogg", AudioParams.Default.WithVolume(-10f));

            var componentManager = IoCManager.Resolve<IComponentManager>();
            foreach (var component in componentManager.EntityQuery<AirlockComponent>()) component.BoltsDown = false;
        }

        public override void Update(float frameTime)
        {
            if (!Running)
            {
                return;
            }

            _elapsedTime += frameTime;

            if (_elapsedTime < _eventDuration)
            {
                return;
            }

            Running = false;
        }
    }
}
