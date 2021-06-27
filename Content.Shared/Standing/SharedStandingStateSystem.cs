#nullable enable
using Content.Shared.EffectBlocker;
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

        public void Down(IEntity entity)
        {
            if (!entity.TryGetComponent(out StandingStateComponent? comp)) return;
            Down(comp);
        }

        public void Stand(IEntity entity)
        {
            if (!entity.TryGetComponent(out StandingStateComponent? comp)) return;
            Stand(comp);
        }

        public void Down(StandingStateComponent component)
        {
            if (!component.Standing) return;

            var entity = component.Owner;
            var uid = entity.Uid;

            // Drop hands regardless unless blocky blocky.
            EntityManager.EventBus.RaiseLocalEvent(uid, new DropHandItemsEvent());

            var msg = new BlockDownEvent();
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);

            if (msg.Cancelled) return;

            component.Standing = false;

            // Seemed like the best place to put it
            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(RotationVisuals.RotationState, RotationState.Horizontal);
            }

            // Currently shit is only downed by server but when it's predicted we can probably only play this on server / client
            var sound = component.DownSoundCollection;

            if (!string.IsNullOrEmpty(sound))
            {
                var file = AudioHelpers.GetRandomFileFromSoundCollection(sound);
                SoundSystem.Play(Filter.Pvs(entity), file, entity, AudioHelpers.WithVariation(0.25f));
            }
        }

        public void Stand(StandingStateComponent component)
        {
            if (component.Standing) return;

            var entity = component.Owner;

            var msg = new BlockStandEvent();
            EntityManager.EventBus.RaiseLocalEvent(entity.Uid, msg);

            if (msg.Cancelled) return;

            component.Standing = true;

            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(RotationVisuals.RotationState, RotationState.Vertical);
            }
        }
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class BlockDownEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class BlockStandEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StandEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownEvent : EntityEventArgs
    {
    }
}
