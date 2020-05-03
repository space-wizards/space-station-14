using System;
using Content.Shared.GameObjects.Components.Weapons;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Weapon
{
    [RegisterComponent]
    public sealed class ServerFlashableComponent : SharedFlashableComponent
    {
        private double _duration;
        private TimeSpan _lastFlash;

        public void Flash(double duration)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            _lastFlash = timing.CurTime;
            _duration = duration;
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new FlashComponentState(_duration, _lastFlash);
        }

        public static void FlashAreaHelper(IEntity source, double range, double duration, string sound = null)
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var component in componentManager.GetAllComponents(typeof(ServerFlashableComponent)))
            {
                if (component.Owner.Transform.MapID != source.Transform.MapID) continue;
                if ((component.Owner.Transform.WorldPosition - source.Transform.WorldPosition).Length < range)
                {
                    var flashable = (ServerFlashableComponent) component;
                    flashable.Flash(duration);
                }
            }

            if (sound != null)
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().Play(sound, source.Transform.GridPosition);
            }
        }
    }
}
