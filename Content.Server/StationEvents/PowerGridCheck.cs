using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class PowerGridCheck : StationEvent
    {
        public override string Name => "PowerGridCheck";

        public override StationEventWeight Weight => StationEventWeight.Normal;

        public override int? MaxOccurrences => 3;

        protected override string StartAnnouncement => Loc.GetString(
            "Abnormal activity detected in the station's powernet. As a precautionary measure, the station's power will be shut off for an indeterminate duration.");

        protected override string EndAnnouncement => Loc.GetString(
            "Power has been restored to the station. We apologize for the inconvenience.");

        private float _elapsedTime;
        private int _failDuration;
        
        private Dictionary<IEntity, bool> _powered = new Dictionary<IEntity, bool>();
        
        public override void Startup()
        {
            base.Startup();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/power_off.ogg");

            _elapsedTime = 0.0f;
            _failDuration = IoCManager.Resolve<IRobustRandom>().Next(30, 120);
            var entityManager = IoCManager.Resolve<IEntityManager>();
            
            foreach (var entity in entityManager.GetEntities(new TypeEntityQuery(typeof(PowerReceiverComponent))))
            {
                var component = entity.GetComponent<PowerReceiverComponent>();
                _powered.Add(entity, component.PowerDisabled);
                component.PowerDisabled = true;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/power_on.ogg");

            foreach (var (entity, powered) in _powered)
            {
                if (entity.TryGetComponent(out PowerReceiverComponent powerReceiverComponent))
                {
                    powerReceiverComponent.PowerDisabled = powered;
                }
            }
            
            _powered.Clear();
        }

        public override void Update(float frameTime)
        {
            if (!Running)
            {
                return;
            }
            
            _elapsedTime += frameTime;

            if (_elapsedTime < _failDuration)
            {
                return;
            }

            Running = false;
        }
    }
}