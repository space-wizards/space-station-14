#nullable enable
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Rotation;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class StandingStateSystem : EntitySystem
    {
        public static bool IsDown(IEntity entity)
        {
            return entity.TryGetComponent(out StandingStateComponent? component) &&
                   component.Standing;
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StandingStateComponent, AttemptStandEvent>(HandleStandAttempt);
            SubscribeLocalEvent<StandingStateComponent, AttemptDownEvent>(HandleDownAttempt);
        }

        private void HandleDownAttempt(EntityUid uid, StandingStateComponent component, AttemptDownEvent args)
        {
            if (!component.Standing || !EntityManager.TryGetEntity(uid, out var entity)) return;

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

        private void HandleStandAttempt(EntityUid uid, StandingStateComponent component, AttemptStandEvent args)
        {
            if (component.Standing || !EntityManager.TryGetEntity(uid, out var entity)) return;

            var msg = new BlockStandEvent();
            EntityManager.EventBus.RaiseLocalEvent(uid, msg);

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
    /// Attempt to knock an entity down.
    /// </summary>
    public sealed class AttemptDownEvent : EntityEventArgs
    {
    }

    // Stando powa
    /// <summary>
    /// Atempt to stand an entity.
    /// </summary>
    public sealed class AttemptStandEvent : EntityEventArgs
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
