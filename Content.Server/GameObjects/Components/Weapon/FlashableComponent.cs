using System;
using System.Linq;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Weapons;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Weapon
{
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
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

        public static void FlashAreaHelper(IEntity source, float range, float duration, string sound = null)
        {
            foreach (var entity in IoCManager.Resolve<IEntityManager>().GetEntitiesInRange(source.Transform.GridPosition, range))
            {
                if (!InteractionChecks.InRangeUnobstructed(source, entity.Transform.MapPosition, range, ignoredEnt:entity))
                    continue;

                if(entity.TryGetComponent(out FlashableComponent flashable))
                    flashable.Flash(duration);
            }

            if (!string.IsNullOrEmpty(sound))
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().PlayAtCoords(sound, source.Transform.GridPosition);
            }
        }
    }
}
