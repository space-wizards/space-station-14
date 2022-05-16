using Content.Server.Body.Systems;
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
using Content.Shared.MobState.State;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, DragonDevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnActionEvent>(OnDragonSpawnAction);

            SubscribeLocalEvent<DragonComponent, DragonDevourComplete>(OnDragonDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourCancelledEvent>(OnDragonDevourCancelled);
            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (component.DeathSound != null && args.CurrentMobState.IsDead())
            {
                SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), component.DeathSound.GetSound());
                component.DragonStomach.EmptyContainer();
            }
        }

        private void OnDragonDevourCancelled(EntityUid uid, DragonComponent component, DragonDevourCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnDragonDevourComplete(EntityUid uid, DragonComponent component, DragonDevourComplete args)
        {
            component.CancelToken = null;
            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            EntityManager.QueueDeleteEntity(args.Target);

            if (component.DevourSound != null)
                SoundSystem.Play(Filter.Pvs(args.User, entityManager: EntityManager), component.DevourSound.GetSound());
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            component.SpawnsLeft = Math.Min(component.SpawnsLeft, component.MaxSpawns);

            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon-stomach");

            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);

            if (component.SpawnAction != null)
                _actionsSystem.AddAction(uid, component.SpawnAction, null);

            // Announces the dragon's spawn with a global bellowing sound
            // NOTE: good idea(?)
            SoundSystem.Play(Filter.Pvs(uid, 4f, EntityManager), "/Audio/Animals/sound_creatures_space_dragon_roar.ogg");
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid dragonuid, DragonComponent component, DragonDevourActionEvent args)
        {
            if (component.CancelToken != null || args.Handled) return;

            args.Handled = true;
            var target = args.Target;
            var ichorInjection = new Solution(component.DevourChem, component.DevourHealRate);
            var halfedIchorInjection = new Solution(component.DevourChem, component.DevourHealRate / 2);

            //Check if the target is valid. The effects should be possible to accomplish on either a wall or a body.
            //Eating bodies is instant, the wall requires a doAfter.

            //NOTE: I honestly don't know much on how one detects valid eating targets, so right now I am using a body component to tell them apart
            //That way dragons can't devour guardians and other dragons. Yet.

            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case SharedCriticalMobState:
                    case SharedDeadMobState:
                        if (!EntityManager.HasComponent<DamageableComponent>(dragonuid)) return;

                        // Recover spawns.
                        component.SpawnsLeft = Math.Min(component.SpawnsLeft + 1, component.MaxSpawns);

                        //Humanoid devours allow dragon to get eggs, corpses included
                        if (EntityManager.HasComponent<HumanoidAppearanceComponent>(target))
                        {
                            // inject the healing chemical into the system.
                            _bloodstreamSystem.TryAddToChemicals(dragonuid, ichorInjection);
                            // Sends the human entity into the stomach so it can be revived later.
                            component.DragonStomach.Insert(target);
                            SoundSystem.Play(Filter.Pvs(dragonuid, entityManager: EntityManager), "/Audio/Effects/sound_magic_demon_consume.ogg");

                        }
                        //Non-humanoid mobs can only heal dragon for half the normal amount
                        else
                        {
                            // heal HALF the damage
                            _bloodstreamSystem.TryAddToChemicals(dragonuid, halfedIchorInjection);
                            // Sends the non-human entity into the stomach
                            // NOTE: I am a bit conflicted on this, and only really adding this because force-delete is bad, is this needed?
                            component.DragonStomach.Insert(target);
                            SoundSystem.Play(Filter.Pvs(dragonuid, entityManager: EntityManager), "/Audio/Effects/sound_magic_demon_consume.ogg");
                        }

                        return;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), dragonuid, Filter.Entities(dragonuid));
                        break;
                }

                return;
            }

            // If it can be built- it can be destroyed
            if (_tagSystem.HasTag(target, "RCDDeconstructWhitelist"))
            {
                _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), dragonuid, Filter.Entities(dragonuid));
                component.CancelToken = new CancellationTokenSource();

                _doAfterSystem.DoAfter(new DoAfterEventArgs(dragonuid, component.DevourTime, component.CancelToken.Token, target)
                {
                    UserFinishedEvent = new DragonDevourComplete(dragonuid, target),
                    UserCancelledEvent = new DragonDevourCancelledEvent(),
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                });
            }
        }

        private void OnDragonSpawnAction(EntityUid dragonuid, DragonComponent component, DragonSpawnActionEvent args)
        {
            if (component.SpawnPrototype == null) return;

            // If dragon has spawns then add one.
            if (component.SpawnsLeft > 0)
            {
                Spawn(component.SpawnPrototype, Transform(dragonuid).Coordinates);
                component.SpawnsLeft--;
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("dragon-spawn-action-popup-message-fail-no-eggs"), dragonuid, Filter.Entities(dragonuid));
        }

        private sealed class DragonDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonDevourComplete(EntityUid user, EntityUid target)
            {
                 User = user;
                 Target = target;
            }
        }

        private sealed class DragonDevourCancelledEvent : EntityEventArgs {}
    }
}
