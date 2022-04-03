using Content.Server.Body.Components;
using Content.Server.Destructible;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Robust.Shared.Player;
using System.Threading;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, DevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<TargetDevourSuccessfulEvent>(OnDevourSucsessful);
            SubscribeLocalEvent<TargetDevourCancelledEvent>(OnDevourCancelled);
        }

        /// <summary>
        /// On cancellation of a devour attempt
        /// </summary>
        private void OnDevourCancelled(TargetDevourCancelledEvent args)
        {
            args.DragonComponent.CancelToken = null;
        }

        /// <summary>
        /// On a sucsessful devour attempt
        /// </summary>
        private void OnDevourSucsessful(TargetDevourSuccessfulEvent args)
        {
            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            EntityManager.QueueDeleteEntity(args.Target);
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid uid, DragonComponent dragoncomp, PerformEntityTargetActionEvent args)
        {
            var target = args.Target;

            //Check if the target is valid. The effects should be possible to accomplish on either a wall or a body.
            //Eating bodies is instant, the wall requires a doAfter.

            //NOTE: I honestly don't know much on how one detects valid eating targets, so right now I am using a body component to tell them apart
            //That way dragons can't devour guardians and other dragons. Yet.

            if (EntityManager.TryGetComponent(target, out BodyComponent body))
            {
                if (EntityManager.TryGetComponent(target, out MobStateComponent targetstate))
                {
                    if (targetstate.CurrentState != null)
                    {
                        //You can only devour dead or crit targets
                        if (targetstate.CurrentState.IsIncapacitated())
                        {
                            //Humanoid devours allow dragon to get eggs, corpses included
                            if (EntityManager.TryGetComponent(target, out HumanoidAppearanceComponent humanoid))
                            {
                                dragoncomp.EggsLeft++;
                                EntityManager.QueueDeleteEntity(target);
                            }
                        }
                        else _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, Filter.Entities(uid));
                        return;
                    }
                    else return;
                }
                else _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-no-body"), uid, Filter.Entities(uid));
                return;
            }
            // If it is a structure it goes through the motions of DoAfter
            else if (EntityManager.TryGetComponent(target, out TagComponent tags))
            {
                // If it can be built- it can be destoryed
                if (tags.Tags.Contains("Wall"))
                {
                    _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, Filter.Entities(uid));
                    dragoncomp.CancelToken = new CancellationTokenSource();
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, dragoncomp.DevourTimer, dragoncomp.CancelToken.Token, target)
                    {
                        BroadcastFinishedEvent = new TargetDevourSuccessfulEvent(uid, target),
                        BroadcastCancelledEvent = new TargetDevourCancelledEvent(dragoncomp),
                        BreakOnTargetMove = true,
                        BreakOnUserMove = true,
                        BreakOnStun = true,
                    });
                }
                else return;
            }
            else return;
        }

        private sealed class TargetDevourSuccessfulEvent : EntityEventArgs
        { 
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public TargetDevourSuccessfulEvent(EntityUid user, EntityUid target)
            {
                 User = user;
                 Target = target;
            }
        }

        private sealed class TargetDevourCancelledEvent : EntityEventArgs
        {
            public readonly DragonComponent DragonComponent;

            public TargetDevourCancelledEvent(DragonComponent dragon)
            {
                DragonComponent = dragon;
            }
        }
    }
}
