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
using Content.Shared.Doors.Components;

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
            if (args.CurrentMobState.IsDead())
            {
                if (component.SoundDeath != null)
                    SoundSystem.Play(component.SoundDeath.GetSound(), Filter.Pvs(uid, entityManager: EntityManager));

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

            if (component.SoundDevour != null)
                SoundSystem.Play(component.SoundDevour.GetSound(), Filter.Pvs(args.User, entityManager: EntityManager));
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            component.SpawnsLeft = Math.Min(component.SpawnsLeft, component.MaxSpawns);

            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon_stomach");

            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);

            if (component.SpawnAction != null)
                _actionsSystem.AddAction(uid, component.SpawnAction, null);

            if (component.SoundRoar != null)
                SoundSystem.Play(component.SoundRoar.GetSound(), Filter.Pvs(uid, 4f, EntityManager));
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

            //Check if the target is valid. The effects should be possible to accomplish on either a wall or a body.
            //Eating bodies is instant, the wall requires a doAfter.

            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case SharedCriticalMobState:
                    case SharedDeadMobState:
                        if (!EntityManager.HasComponent<DamageableComponent>(dragonuid)) return;

                        //Humanoid devours allow dragon to get eggs, corpses included
                        if (EntityManager.HasComponent<HumanoidAppearanceComponent>(target))
                        {
                            // Add a spawn for a consumed humanoid
                            component.SpawnsLeft = Math.Min(component.SpawnsLeft + 1, component.MaxSpawns);
                        }
                        //Non-humanoid mobs can only heal dragon for half the normal amount, with no additional spawn tickets
                        else
                        {
                            ichorInjection.ScaleSolution(0.5f);
                        }

                        _bloodstreamSystem.TryAddToChemicals(dragonuid, ichorInjection);
                        component.DragonStomach.Insert(target);

                        if (component.SoundDevour != null)
                            SoundSystem.Play(component.SoundDevour.GetSound(), Filter.Pvs(dragonuid, entityManager: EntityManager));

                        return;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), dragonuid, Filter.Entities(dragonuid));
                        break;
                }

                return;
            }

            // Absolutely ass solution but requires less yaml fuckery
            // If it's a door (firelock, airlock, windoor), Wall or Window, dragon can eat it.
            if (_tagSystem.HasTag(target, "Wall") || (_tagSystem.HasTag(target, "Window") || EntityManager.HasComponent<DoorComponent>(target)))
            {
                _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), dragonuid, Filter.Entities(dragonuid));

                if (component.SoundStructureDevour != null)
                    SoundSystem.Play(component.SoundStructureDevour.GetSound(), Filter.Pvs(dragonuid, entityManager: EntityManager));

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
