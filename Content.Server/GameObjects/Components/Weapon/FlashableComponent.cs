using System;
using Content.Shared.GameObjects.Components.Weapons;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Weapon
{
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private double _duration;
        private TimeSpan _lastFlash;

        public void Flash(double duration)
        {
            _lastFlash = _gameTiming.CurTime;
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
                if (!source.InRangeUnobstructed(entity, range, popup: true))
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
