using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Hands.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    public sealed class CuffableSystem : SharedCuffableSystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);

            SubscribeLocalEvent<CuffableComponent, GetOtherVerbsEvent>(AddUncuffVerb);
        }

        private void AddUncuffVerb(EntityUid uid, CuffableComponent component, GetOtherVerbsEvent args)
        {
            // Can the user access the cuffs, and is there even anything to uncuff?
            if (!args.CanAccess || component.CuffedHandCount == 0)
                return;

            // We only check can interact if the user is not uncuffing themselves. As a result, the verb will show up
            // when the user is incapacitated & trying to uncuff themselves, but TryUncuff() will still fail when
            // attempted.
            if (args.User != args.Target && !args.CanInteract)
                return;

            Verb verb = new();
            verb.Act = () => component.TryUncuff(args.User);
            verb.Text = Loc.GetString("uncuff-verb-get-data-text");
            //TODO VERB ICON add uncuffing symbol? may re-use the alert symbol showing that you are currently cuffed?
            args.Verbs.Add(verb);
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
                // This UncuffAttemptEvent check should probably be In MobStateSystem, not here?
                if (userEntity.TryGetComponent<MobStateComponent>(out var state))
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
                    // TODO CUFFABLE/STUN add UncuffAttemptEvent subscription to StunSystem
                }
            }
            else
            {
                // Check if the user can interact.
                if (!_actionBlockerSystem.CanInteract(userEntity.Uid))
                {
                    args.Cancel();
                }
            }
            if (args.Cancelled)
            {
                _popupSystem.PopupEntity(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, Filter.Entities(userEntity.Uid));
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
