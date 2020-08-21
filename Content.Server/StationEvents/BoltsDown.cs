using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Server.Interfaces.Player;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class BoltsDown : StationEvent
    {
        public override string Name => "BoltsDown";

        public override StationEventWeight Weight => StationEventWeight.Normal;

        public override int? MaxOccurrences => 1;

        protected override string StartAnnouncement => Loc.GetString(
            "The airlocks have been hacked by the hacker known as 4chan. We have dispatched hacking equipment to the crew so that you can continue your productive shift");

         protected override string EndAnnouncement => Loc.GetString(
             "Our team of lawyers have dealt with the problem. Have a nice shift.");

        private float _elapsedTime;
        private int _eventDuration;        
        public override void Startup()
        {
            base.Startup();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Effects/alert.ogg");

            _eventDuration = IoCManager.Resolve<IRobustRandom>().Next(180, 600);
            var componentManager = IoCManager.Resolve<IComponentManager>();
            
            foreach (var component in componentManager.EntityQuery<AirlockComponent>())
            {
                component.BoltsDown = true;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            foreach (var player in playerManager.GetAllPlayers())
            {
                var playerPos = player.AttachedEntity.Transform.GridPosition;
                var entityManager = IoCManager.Resolve<IEntityManager>();
                entityManager.SpawnEntity("UtilityBeltClothingFilled", playerPos);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/power_on.ogg");
            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var component in componentManager.EntityQuery<AirlockComponent>())
            {
                component.BoltsDown = false;
            }
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