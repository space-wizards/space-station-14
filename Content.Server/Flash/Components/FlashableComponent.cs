using Content.Shared.Flash;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Flash.Components
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent, Friend(typeof(FlashSystem))]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
<<<<<<< HEAD
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private double _duration;
        private TimeSpan _lastFlash;

        public void Flash(double duration)
        {
            _lastFlash = _gameTiming.CurTime;
            _duration = duration;
            Dirty();
        }
            
        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new FlashComponentState(_duration, _lastFlash);
        }

        public static void FlashAreaHelper(IEntity source, float range, float duration, SoundSpecifier? sound = null)
        {
            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(source.Transform.Coordinates, range))
            {
                if (!entity.TryGetComponent(out FlashableComponent? flashable) ||
                    !source.InRangeUnobstructed(entity, range, CollisionGroup.Opaque)) continue;

                flashable.Flash(duration);
            }

            if (sound != null)
            {
                SoundSystem.Play(Filter.Pvs(source), sound.GetSound(), source.Transform.Coordinates);
            }
        }
=======
>>>>>>> 22cc42ff502bafbcc3ed8fe924b5d53bdcd9a412
    }
}
