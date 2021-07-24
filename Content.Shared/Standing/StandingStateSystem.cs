using Content.Shared.Audio;
using Content.Shared.Hands;
using Content.Shared.Rotation;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {
        public bool IsDown(IEntity entity)
        {
            if (entity.TryGetComponent(out StandingStateComponent? standingState) &&
                standingState.Standing) return true;

            return false;
        }

        public void Down(IEntity entity, bool playSound = true, bool dropHeldItems = true)
        {
            if (!entity.TryGetComponent(out StandingStateComponent? comp)) return;
            Down(comp, playSound, dropHeldItems);
        }

        public void Stand(IEntity entity)
        {
            if (!entity.TryGetComponent(out StandingStateComponent? comp)) return;
            Stand(comp);
        }

        public void Down(StandingStateComponent component, bool playSound = true, bool dropHeldItems = true)
        {
            if (!component.Standing) return;

            var entity = component.Owner;
            var uid = entity.Uid;

            // This is just to avoid most callers doing this manually saving boilerplate
            // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
            // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
            // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
            if (dropHeldItems)
            {
                Get<SharedHandsSystem>().DropHandItems(entity, false);
            }

            var msg = new DownAttemptEvent();
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);

            if (msg.Cancelled) return;

            component.Standing = false;
            component.Dirty();
            EntityManager.EventBus.RaiseLocalEvent(uid, new DownedEvent());

            // Seemed like the best place to put it
            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(RotationVisuals.RotationState, RotationState.Horizontal);
            }

            // Currently shit is only downed by server but when it's predicted we can probably only play this on server / client
            var sound = component.DownSoundCollection;

            if (playSound && !string.IsNullOrEmpty(sound))
            {
                var file = AudioHelpers.GetRandomFileFromSoundCollection(sound);
                SoundSystem.Play(Filter.Pvs(entity), file, entity, AudioHelpers.WithVariation(0.25f));
            }
        }

        public void Stand(StandingStateComponent component)
        {
            if (component.Standing) return;

            var entity = component.Owner;
            var uid = entity.Uid;

            var msg = new StandAttemptEvent();
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);

            if (msg.Cancelled) return;

            component.Standing = true;
            component.Dirty();
            EntityManager.EventBus.RaiseLocalEvent(uid, new StoodEvent());

            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(RotationVisuals.RotationState, RotationState.Vertical);
            }
        }
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class DownAttemptEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class StandAttemptEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StoodEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownedEvent : EntityEventArgs
    {
    }
}
