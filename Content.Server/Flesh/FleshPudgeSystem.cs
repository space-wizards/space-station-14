using Content.Server.Actions;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;

namespace Content.Server.Flesh
{
    public sealed class FleshPudgeSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly GunSystem _gunSystem = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FleshPudgeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeThrowWormActionEvent>(OnThrowWorm);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeAbsorbBloodPoolActionEvent>(OnAbsorbBloodPoolActionEvent);
            SubscribeLocalEvent<FleshPudgeComponent, FleshPudgeAcidSpitActionEvent>(OnAcidSpit);
        }

        public sealed class FleshPudgeThrowWormActionEvent : WorldTargetActionEvent
        {

        }

        public sealed class FleshPudgeAcidSpitActionEvent : WorldTargetActionEvent
        {

        }

        public sealed class FleshPudgeAbsorbBloodPoolActionEvent : InstantActionEvent
        {

        }

        private void OnThrowWorm(EntityUid uid, FleshPudgeComponent component, FleshPudgeThrowWormActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var worm = Spawn(component.WormMobSpawnId, Transform(uid).Coordinates);
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(EntityManager);
            var direction = mapCoords.Position - xform.MapPosition.Position;

            _throwing.TryThrow(worm, direction, 7F, uid, 10F);
            if (component.SoundThrowWorm != null)
            {
                _audioSystem.PlayPvs(component.SoundThrowWorm, uid, component.SoundThrowWorm.Params);
            }
            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-popup"), uid, PopupType.LargeCaution);
        }

        private void OnAcidSpit(EntityUid uid, FleshPudgeComponent component, FleshPudgeAcidSpitActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var acidBullet = Spawn(component.BulletAcidSpawnId, Transform(uid).Coordinates);
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(EntityManager);
            var direction = mapCoords.Position - xform.MapPosition.Position;
            var userVelocity = _physics.GetMapLinearVelocity(uid);

            _gunSystem.ShootProjectile(acidBullet, direction, userVelocity, uid, uid);
            _audioSystem.PlayPvs(component.BloodAbsorbSound, uid, component.BloodAbsorbSound.Params);
        }

        private void OnAbsorbBloodPoolActionEvent(EntityUid uid, FleshPudgeComponent component,
            FleshPudgeAbsorbBloodPoolActionEvent args)
        {
            if (args.Handled)
                return;

            var xform = Transform(uid);
            var puddles = new ValueList<(EntityUid Entity, string Solution)>();
            puddles.Clear();
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, 0.5f))
            {
                if (TryComp<PuddleComponent>(entity, out var puddle))
                {
                    puddles.Add((entity, puddle.SolutionName));
                }
            }

            if (puddles.Count == 0)
            {
                _popup.PopupEntity(Loc.GetString("flesh-cultist-not-find-puddles"),
                    uid, uid, PopupType.Large);
                return;
            }

            var totalBloodQuantity = new float();

            foreach (var (puddle, solution) in puddles)
            {
                if (!_solutionSystem.TryGetSolution(puddle, solution, out var puddleSolution))
                {
                    continue;
                }
                var hasImpurities = false;
                var pudleBloodQuantity = new FixedPoint2();
                foreach (var puddleSolutionContent in puddleSolution.Contents.ToArray())
                {
                    if (puddleSolutionContent.ReagentId != "Blood")
                    {
                        hasImpurities = true;
                    }
                    else
                    {
                        pudleBloodQuantity += puddleSolutionContent.Quantity;
                    }
                }
                if (hasImpurities)
                    continue;
                totalBloodQuantity += pudleBloodQuantity.Float();
                QueueDel(puddle);
            }

            if (totalBloodQuantity == 0)
            {
                _popup.PopupEntity(Loc.GetString("flesh-cultist-cant-absorb-puddle"),
                    uid, uid, PopupType.Large);
                return;
            }

            _audioSystem.PlayPvs(component.BloodAbsorbSound, uid, component.BloodAbsorbSound.Params);
            _popup.PopupEntity(Loc.GetString("flesh-cultist-absorb-puddle", ("Entity", uid)),
                uid, uid, PopupType.Large);

            var transferSolution = new Solution();
            foreach (var reagent in component.HealBloodAbsorbReagents.ToArray())
            {
                transferSolution.AddReagent(reagent.ReagentId, reagent.Quantity * (totalBloodQuantity / 10));
            }
            if (_solutionSystem.TryGetInjectableSolution(uid, out var injectableSolution))
            {
                _solutionSystem.TryAddSolution(uid, injectableSolution, transferSolution);
            }
            args.Handled = true;
        }

        private void OnStartup(EntityUid uid, FleshPudgeComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionAcidSpit, null);
            _action.AddAction(uid, component.ActionThrowWorm, null);
            _action.AddAction(uid, component.ActionAbsorbBloodPool, null);
        }
    }
}
