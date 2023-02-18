using Content.Shared.Physics;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding;

using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;

using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server.Abilities.SCP.ConcreteSlab
{
    public sealed class SCP173System : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;


        private Dictionary<EntityUid, SCP173Component> _scpList = new();
        private Dictionary<EntityUid, TimeSpan> _spooksCD = new();
        private TimeSpan _spookCooldown = TimeSpan.FromSeconds(10);

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SCP173Component, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SCP173Component, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<SCP173Component, AttackAttemptEvent>(OnTryAttack);


            SubscribeLocalEvent<SCP173Component, ShartActionEvent>(OnShartAction);
            SubscribeLocalEvent<SCP173Component, BlindActionEvent>(OnBlindAction);

        }

        private void OnComponentInit(EntityUid uid, SCP173Component component, ComponentInit args)
        {
            _scpList[uid] = component;
            _actionsSystem.AddAction(uid, component.ShartAction, uid);
            _actionsSystem.AddAction(uid, component.BlindAction, uid);
            component.BlindAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(240));
        }

        private void OnComponentRemove(EntityUid uid, SCP173Component component, ComponentRemove args)
        {
            _scpList.Remove(uid);
            _actionsSystem.RemoveAction(uid, component.ShartAction);
            _actionsSystem.RemoveAction(uid, component.BlindAction);
            OnStopLookedAt(uid, component);
        }

        private void OnTryAttack(EntityUid uid, SCP173Component component, AttackAttemptEvent args)
        {
            if (component.LookedAt) { args.Cancel(); return; }
            var target = args.Target;
            if (!(target.HasValue && HasComp<MobStateComponent>(target.Value))) { args.Cancel(); return; }
            if (_mobState.IsDead(target.Value)) { args.Cancel(); return; }
            _audio.PlayPvs(component.KillSound, target.Value);
        }

        private void OnShartAction(EntityUid uid, SCP173Component component, ShartActionEvent args)
        {
            if (!component.Enabled) return;
            Solution solution = new("Blood", 5);
            solution.AddReagent("Nutriment", 8);
            _spillableSystem.SpillAt(uid, solution, "SCP173Puddle");

            args.Handled = true;
        }

        private void OnBlindAction(EntityUid uid, SCP173Component component, BlindActionEvent args)
        {
            foreach (var ply in component.Lookers)
                _statusEffectsSystem.TryAddStatusEffect(ply, SharedBlindingSystem.BlindingStatusEffect, TimeSpan.FromSeconds(10), true, "TemporaryBlindness");

            _audio.PlayPvs(component.ScaresSound, uid);

            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach(var keyVar in _scpList)
            {
                var ent = keyVar.Key;
                var comp = keyVar.Value;
                var wasLookedAt = comp.LookedAt;
                var isLookedAt = IsLookedAt(ent, comp.EyeSightRange,ref comp.Lookers, out var newLookers);

                if (newLookers.Count > 0)
                {
                    var validSpooks = new List<EntityUid>();
                    var curTime = _gameTiming.CurTime;
                    foreach (var ply in newLookers)
                    {
                        if (_spooksCD.TryGetValue(ply, out var time) && time > curTime) continue;
                        validSpooks.Add(ply);
                        _spooksCD[ply] = curTime + _spookCooldown;
                    }
                    if (validSpooks.Count > 0)
                        _audio.Play(comp.SpooksSound, Filter.Entities(validSpooks.ToArray()), ent, false);
                }

                if (isLookedAt == wasLookedAt) continue;
                comp.LookedAt = isLookedAt;
                if (isLookedAt) OnStartLookedAt(ent, comp);
                    else OnStopLookedAt(ent, comp);
            }
        }

        private void OnStartLookedAt(EntityUid uid, SCP173Component comp)
        {
            if (TryComp<InputMoverComponent>(uid, out var input)) input.CanMove = false;
            if (TryComp<PhysicsComponent>(uid, out var phys)) _physics.SetFixedRotation(uid, true);
        }
        private void OnStopLookedAt(EntityUid uid, SCP173Component comp)
        {
            if (TryComp<InputMoverComponent>(uid, out var input)) input.CanMove = true;
            if (TryComp<PhysicsComponent>(uid, out var phys)) _physics.SetFixedRotation(uid, false);
        }

        public bool IsLookedAt(EntityUid uid, float range, ref List<EntityUid> lookers, out List<EntityUid> newLooks)
        {
            var oldLooks = new List<EntityUid>(lookers);
            lookers.Clear();
            var xform = Comp<TransformComponent>(uid);
            bool isLookedAt = false;
            newLooks = new();
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, range))
            {
                if ( HasComp<MindComponent>(entity) &&
                    !HasComp<SCP173Component>(entity) &&
                    !HasComp<GhostComponent>(entity) &&
                    _mobState.IsAlive(entity) &&
                    !HasComp<TemporaryBlindnessComponent>(entity) &&
                    IsVisible(uid, entity))
                {
                    lookers.Add(entity);
                    isLookedAt = true;
                    if (!oldLooks.Contains(entity))
                        newLooks.Add(entity);
                }
            }
            return isLookedAt;
        }
        private bool IsVisible(EntityUid self, EntityUid target)
        {
            var slfXForm = Transform(self);
            var slfPos = _transform.GetWorldPosition(slfXForm);
            var trgPos = _transform.GetWorldPosition(Transform(target));
            var locPos = slfPos - trgPos;
            var ray = new CollisionRay(trgPos, locPos.Normalized, (int)CollisionGroup.Opaque);
            var results = _physics.IntersectRay(slfXForm.MapID,ray, locPos.Length,target,false);
            foreach (var result in results)
            {
                var ent = result.HitEntity;
                if (ent == self) return true;
                if (HasComp<OccluderComponent>(ent)) return false;
            }
            return true;
        }
    }
    public sealed class ShartActionEvent : InstantActionEvent { }
    public sealed class BlindActionEvent : InstantActionEvent { }
}
