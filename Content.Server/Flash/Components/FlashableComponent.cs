using System;
using Content.Shared.Flash;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Flash.Components
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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new FlashComponentState(_duration, _lastFlash);
        }

        public static void FlashAreaHelper(IEntity source, float range, float duration, string? sound = null)
        {
            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(source.Transform.Coordinates, range))
            {
                if (!entity.TryGetComponent(out FlashableComponent? flashable) ||
                    !source.InRangeUnobstructed(entity, range, CollisionGroup.Opaque)) continue;

                flashable.Flash(duration);
            }

            if (!string.IsNullOrEmpty(sound))
            {
                SoundSystem.Play(Filter.Pvs(source), sound, source.Transform.Coordinates);
            }
        }
    }
}
