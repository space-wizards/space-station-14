using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.MobState;
using Content.Shared.Cuffs;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.IoC;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    internal sealed class CuffableSystem : SharedCuffableSystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);
        }

        private void OnUncuffAttempt(UncuffAttemptEvent args)
        {
            if (args.Cancelled)
            {
                return;
            }
            if (!EntityManager.TryGetEntity(args.User, out var userEntity))
            {
                // Should this even be possible?
                args.Cancel();
                return;
            }
            // If the user is the target, special logic applies.
            // This is because the CanInteract blocking of the cuffs prevents self-uncuff.
            if (args.User == args.Target)
            {
                if (userEntity.TryGetComponent<IMobStateComponent>(out var state))
                {
                    // Manually check this.
                    if (state.IsIncapacitated())
                    {
                        args.Cancel();
                    }
                }
                else
                {
                    // Uh... let it go through???
                }
            }
            else
            {
                // Check if the user can interact.
                if (!_actionBlockerSystem.CanInteract(userEntity))
                {
                    args.Cancel();
                }
            }
            if (args.Cancelled)
            {
                _popupSystem.PopupEntity(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, _popupSystem.GetFilterFromEntity(userEntity));
            }
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!owner.TryGetComponent(out CuffableComponent? cuffable) ||
                !cuffable.Initialized) return;

            var dirty = false;
            var handCount = owner.GetComponentOrNull<HandsComponent>()?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                entity.Transform.WorldPosition = owner.Transform.WorldPosition;
            }

            if (dirty)
            {
                cuffable.CanStillInteract = handCount > cuffable.CuffedHandCount;
                cuffable.CuffedStateChanged();
                cuffable.Dirty();
            }
        }
    }

    /// <summary>
    /// Event fired on the User when the User attempts to cuff the Target.
    /// Should generate popups on the User.
    /// </summary>
    public class UncuffAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;

        public UncuffAttemptEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
}
