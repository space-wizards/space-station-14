using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Destructible;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Threading;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, DevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, CarpBirthEvent>(OnCarpBirthAction);

            SubscribeLocalEvent<TargetDevourSuccessfulEvent>(OnDevourSucsessful);
            SubscribeLocalEvent<TargetDevourCancelledEvent>(OnDevourCancelled);
            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (args.CurrentMobState.IsDead())
            {
                SoundSystem.Play(Filter.Pvs(uid), "/Audio/Animals/sound_creatures_space_dragon_roar.ogg");
                component.DragonStomach.EmptyContainer();
            }
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
            SoundSystem.Play(Filter.Pvs(args.User), "/Audio/Effects/sound_magic_demon_consume.ogg");
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon-stomach");
            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);
            if (component.CarpBirthAction != null)
                _actionsSystem.AddAction(uid, component.CarpBirthAction, null);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid dragonuid, DragonComponent dragoncomp, PerformEntityTargetActionEvent args)
        {
            var target = args.Target;
            var ichorInjection = new Solution(dragoncomp.DevourChem, dragoncomp.DevourHealRate);
            var halfedIchorInjection = new Solution(dragoncomp.DevourChem, dragoncomp.DevourHealRate / 2);

            //Check if the target is valid. The effects should be possible to accomplish on either a wall or a body.
            //Eating bodies is instant, the wall requires a doAfter.

            //NOTE: I honestly don't know much on how one detects valid eating targets, so right now I am using a body component to tell them apart
            //That way dragons can't devour guardians and other dragons. Yet.

            if (EntityManager.TryGetComponent(target, out MobStateComponent targetstate))
            {
                if (targetstate.CurrentState != null)
                {
                    //You can only devour dead or crit targets
                    if (targetstate.CurrentState.IsIncapacitated())
                    {
                        if (EntityManager.TryGetComponent(dragonuid, out DamageableComponent dragonhealth))
                        {
                            //Humanoid devours allow dragon to get eggs, corpses included
                            if (EntityManager.TryGetComponent(target, out HumanoidAppearanceComponent humanoid))
                            {
                                dragoncomp.EggsLeft++;
                                //inject the healing chemical into the system. Yes the dragon just fucking drinks his own blood.
                                _bloodstreamSystem.TryAddToChemicals(dragonuid, ichorInjection);
                                //Sends the human entity into the stomach so it can be revived later. Withold further comments.
                                dragoncomp.DragonStomach.Insert(target);
                                SoundSystem.Play(Filter.Pvs(dragonuid), "/Audio/Effects/sound_magic_demon_consume.ogg");

                            }
                            //Non-humanoid mobs can only heal dragon for half the normal amount
                            else
                            {
                                //heal HALF the damage
                                _bloodstreamSystem.TryAddToChemicals(dragonuid, halfedIchorInjection);
                                //Sends the non-human entity into the stomach
                                //NOTE: I am a bit conflicted on this, and only really adding this because force-delete is bad, is this needed?
                                dragoncomp.DragonStomach.Insert(target);
                                SoundSystem.Play(Filter.Pvs(dragonuid), "/Audio/Effects/sound_magic_demon_consume.ogg");
                            }
                        }
                        else return;

                    }
                    else _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), dragonuid, Filter.Entities(dragonuid));
                    return;
                }
                else return;
            }
            // If it is a structure it goes through the motions of DoAfter
            else if (EntityManager.TryGetComponent(target, out TagComponent tags))
            {
                // If it can be built- it can be destoryed
                if (tags.Tags.Contains("RCDDeconstructWhitelist"))
                {
                    _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), dragonuid, Filter.Entities(dragonuid));
                    dragoncomp.CancelToken = new CancellationTokenSource();
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(dragonuid, dragoncomp.DevourTimer, dragoncomp.CancelToken.Token, target)
                    {
                        BroadcastFinishedEvent = new TargetDevourSuccessfulEvent(dragonuid, target),
                        BroadcastCancelledEvent = new TargetDevourCancelledEvent(dragoncomp),
                        BreakOnTargetMove = true,
                        BreakOnUserMove = true,
                        BreakOnStun = true,
                    });
                }
                else return;
            }
            else _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-no-body"), dragonuid, Filter.Entities(dragonuid));
            return;
        }

        private void OnCarpBirthAction(EntityUid dragonuid, DragonComponent component, CarpBirthEvent args)
        {
            //If dragon has eggs, remove one, spawn carp
            if (component.EggsLeft > 0)
            {
                EntityManager.SpawnEntity(component.CarpProto, Transform(dragonuid).Coordinates);
                component.EggsLeft--;
            }
            else _popupSystem.PopupEntity(Loc.GetString("birth-carp-action-popup-message-fail-no-eggs"), dragonuid, Filter.Entities(dragonuid));
            return;
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
