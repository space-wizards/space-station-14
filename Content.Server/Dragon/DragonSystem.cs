using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Threading;
using Content.Server.Projectiles;
using Content.Server.Projectiles.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly ProjectileSystem _projectileSystem = default!;
        [Dependency] private readonly IGameTiming _timingSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
        [Dependency] private readonly IRobustRandom _randomSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        private readonly Dictionary<EntityUid, DragonFirebreath> _pendingBreathAttacks = new();

        private const string DefaultProjectilePrototype = "DragonProjectileFireball";


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, DragonDevourComplete>(OnDragonDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnActionEvent>(OnDragonSpawnAction);
            SubscribeLocalEvent<DragonComponent, DragonBreatheFireActionEvent>(OnDragonBreathFire);
            SubscribeLocalEvent<DragonComponent, DragonStructureDevourComplete>(OnDragonStructureDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourCancelledEvent>(OnDragonDevourCancelled);
            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DragonComponent, RefreshMovementSpeedModifiersEvent>(OnDragonRefreshMovespeed);
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (args.CurrentMobState == DamageState.Dead)
            {
                if (component.SoundDeath != null)
                    _audioSystem.PlayPvs(component.SoundDeath, uid, component.SoundDeath.Params);

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
            var ichorInjection = new Solution(component.DevourChem, component.DevourHealRate);

            //Humanoid devours allow dragon to get eggs, corpses included
            if (EntityManager.HasComponent<HumanoidAppearanceComponent>(args.Target))
            {
                // Add a spawn for a consumed humanoid
                component.SpawnsLeft = Math.Min(component.SpawnsLeft + 1, component.MaxSpawns);
            }
            //Non-humanoid mobs can only heal dragon for half the normal amount, with no additional spawn tickets
            else
            {
                ichorInjection.ScaleSolution(0.5f);
            }

            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
            component.DragonStomach.Insert(args.Target);

            if (component.SoundDevour != null)
                _audioSystem.PlayPvs(component.SoundDevour, uid, component.SoundDevour.Params);
        }

        private void OnDragonStructureDevourComplete(EntityUid uid, DragonComponent component, DragonStructureDevourComplete args)
        {
            component.CancelToken = null;
            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            EntityManager.QueueDeleteEntity(args.Target);

            if (component.SoundDevour != null)
                _audioSystem.PlayPvs(component.SoundDevour, uid, component.SoundDevour.Params);
        }

        private void OnDragonRefreshMovespeed(EntityUid uid, DragonComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (!_pendingBreathAttacks.ContainsKey(uid))
                return;

            args.ModifySpeed(0.5f, 0.5f);
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

            if(component.BreatheFireAction != null)
                _actionsSystem.AddAction(uid, component.BreatheFireAction, null);

            if (component.SoundRoar != null)
                _audioSystem.Play(component.SoundRoar, Filter.Pvs(uid, 4f, EntityManager), uid, component.SoundRoar.Params);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid uid, DragonComponent component, DragonDevourActionEvent args)
        {
            if (component.CancelToken != null ||
                args.Handled ||
                component.DevourWhitelist?.IsValid(args.Target, EntityManager) != true)
            {
                return;
            }


            args.Handled = true;
            var target = args.Target;

            // Structure and mob devours handled differently.
            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case DamageState.Critical:
                    case DamageState.Dead:
                        component.CancelToken = new CancellationTokenSource();

                        _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.DevourTime, component.CancelToken.Token, target)
                        {
                            UserFinishedEvent = new DragonDevourComplete(uid, target),
                            UserCancelledEvent = new DragonDevourCancelledEvent(),
                            BreakOnTargetMove = true,
                            BreakOnUserMove = true,
                            BreakOnStun = true,
                        });
                        break;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, Filter.Entities(uid));
                        break;
                }

                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, Filter.Entities(uid));

            if (component.SoundStructureDevour != null)
                _audioSystem.PlayPvs(component.SoundStructureDevour, uid, component.SoundStructureDevour.Params);

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.StructureDevourTime, component.CancelToken.Token, target)
            {
                UserFinishedEvent = new DragonStructureDevourComplete(uid, target),
                UserCancelledEvent = new DragonDevourCancelledEvent(),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
            });
        }

        private void OnDragonSpawnAction(EntityUid dragonuid, DragonComponent component, DragonSpawnActionEvent args)
        {
            if (component.SpawnPrototype == null)
                return;

            // If dragon has spawns then add one.
            if (component.SpawnsLeft > 0)
            {
                Spawn(component.SpawnPrototype, Transform(dragonuid).Coordinates);
                component.SpawnsLeft--;
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("dragon-spawn-action-popup-message-fail-no-eggs"), dragonuid, Filter.Entities(dragonuid));
        }

        private void OnDragonBreathFire(EntityUid dragonuid, DragonComponent component,
            DragonBreatheFireActionEvent args)
        {
            if (args.Handled || component.BreatheFireAction == null || _pendingBreathAttacks.ContainsKey(dragonuid))
                return;

            var dragonXform = Transform(dragonuid);
            var dragonPos = dragonXform.MapPosition;

            var breathDirection = (args.Target.Position - dragonPos.Position);
            if (breathDirection == Vector2.Zero || breathDirection == Vector2.NaN)
                return;

            _pendingBreathAttacks.Add(dragonuid, new DragonFirebreath()
                    { BreathsRemaining = 5, MapDirection = breathDirection.Normalized, NextBreath = TimeSpan.Zero, BreathPrototype = component.BreathProjectilePrototype ?? DefaultProjectilePrototype});

            args.Handled = true;

            if (component.SoundBreathFire != null)
                _audioSystem.PlayPvs(component.SoundBreathFire, dragonuid, component.SoundBreathFire.Params);
        }

        public override void Update(float frameTime)
        {
            foreach (var (uid, breath) in _pendingBreathAttacks)
            {
                if (breath.NextBreath > _timingSystem.CurTime)
                    continue;

                if (!TryComp<TransformComponent>(uid, out var xform))  // TODO: xform query
                    return;

                var random = _randomSystem.NextFloat(-1.0f, 1.0f);
                var spread = Angle.FromDegrees(25f * random);
                var angle = breath.MapDirection.ToWorldAngle() + spread;
                var direction = angle.ToWorldVec();

                var breathSpawn = xform.MapPosition.Offset(direction * 0.8f);
                var breathProjectileUid = Spawn(breath.BreathPrototype, breathSpawn);
                ShootBreathProjectile(breathProjectileUid, direction, uid, breath.Speed);

                breath.NextBreath = _timingSystem.CurTime + breath.Delay;
                breath.BreathsRemaining--;

                if (breath.BreathsRemaining <= 0)
                    _pendingBreathAttacks.Remove(uid);

                // TODO: Only need to run this for first and last breath.
                _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
            }
        }

        private void ShootBreathProjectile(EntityUid uid, Vector2 direction, EntityUid user, float speed)
        {
            var physics = EnsureComp<PhysicsComponent>(uid);
            physics.BodyStatus = BodyStatus.InAir;
            physics.LinearVelocity = direction * speed;

            var projectile = EnsureComp<ProjectileComponent>(uid);
            _projectileSystem.SetShooter(projectile, user);

            Transform(uid).WorldRotation = direction.ToWorldAngle();
        }

        private sealed class DragonFirebreath
        {
            public TimeSpan NextBreath;

            public int BreathsRemaining;

            public Vector2 MapDirection;

            public float Speed = 6.5f;

            public TimeSpan Delay = TimeSpan.FromMilliseconds(200);

            public string BreathPrototype = DefaultProjectilePrototype;
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

        private sealed class DragonStructureDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonStructureDevourComplete(EntityUid user, EntityUid target)
            {
                 User = user;
                 Target = target;
            }
        }

        private sealed class DragonDevourCancelledEvent : EntityEventArgs {}
    }
}
