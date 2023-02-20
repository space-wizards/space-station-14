using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

using Content.Shared.Physics;
using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding;
using Content.Shared.Tag;
using Content.Shared.SCP.ConcreteSlab;


using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Administration.Events;
using Content.Server.Ghost;
using Content.Server.GameTicking;
using Content.Shared.Doors.Components;
using Content.Server.Doors.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.SCP.ConcreteSlab
{
    public sealed class SCP173System : SharedSCP173System
    {
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly TagSystem _tagSys = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly ActionsSystem _actionSys = default!;
        [Dependency] private readonly DoorSystem _doorSys = default!;


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
            SubscribeLocalEvent<SCP173Component, DoorOpenActionEvent>(OnDoorOpenAction);
        }

        private void OnComponentInit(EntityUid uid, SCP173Component component, ComponentInit args)
        {
            _scpList[uid] = component;
            _actionSys.AddAction(uid, component.ShartAction, uid);
            _actionSys.AddAction(uid, component.DoorOpenAction, uid);
            component.BlindAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(240));
        }
        private void OnComponentRemove(EntityUid uid, SCP173Component component, ComponentRemove args)
        {
            _scpList.Remove(uid);
            _actionSys.RemoveAction(uid, component.ShartAction);
            _actionSys.RemoveAction(uid, component.BlindAction);
            _actionSys.RemoveAction(uid, component.DoorOpenAction);
            OnStopLookedAt(uid, component);
        }

        private void OnTryAttack(EntityUid uid, SCP173Component component, AttackAttemptEvent args)
        {
            var target = args.Target.GetValueOrDefault();
            if (!CanAttack(uid, target, component)) { args.Cancel(); return; }
            _audio.PlayPvs(component.KillSound, target);
        }

        #region Actions
        private void OnShartAction(EntityUid uid, SCP173Component component, ShartActionEvent args)
        {
            if (!component.Enabled) return;
            Solution solution = new("Nutriment", 8);
            solution.AddReagent("Blood", 5);
            _spillableSystem.SpillAt(uid, solution, "SCP173Puddle");

            args.Handled = true;
        }
        private void OnBlindAction(EntityUid uid, SCP173Component component, BlindActionEvent args)
        {
            if (!component.Enabled) return;

            foreach (var ent in _lookup.GetEntitiesInRange(uid, 6))
            {
                var ghostBoo = new GhostBooEvent();
                if (HasComp<PointLightComponent>(ent))
                {
                    RaiseLocalEvent(ent, ghostBoo, true);
                }
            }
            Timer.Spawn(3000, () =>
            {
                if (_gameTicker.RunLevel != GameRunLevel.InRound || !uid.IsValid() || component == null)
                    return;
                var lookerList = component.Lookers;
                var coords = Comp<TransformComponent>(uid).Coordinates;
                foreach (var ply in _lookup.GetEntitiesInRange(uid, 10))
                {
                    if (IsValidObserver(ply))
                    {
                        var plyCords = Comp<TransformComponent>(ply).Coordinates;
                        if (coords.TryDistance(_entMan, plyCords, out var dist))
                            _statusEffectsSystem.TryAddStatusEffect(ply, SharedBlindingSystem.BlindingStatusEffect,
                                TimeSpan.FromSeconds(lookerList.Contains(ply) ? 8 : 12 - dist), true, "TemporaryBlindness");
                        _audio.PlayStatic(component.ScaresSound, ply, coords);
                    }
                }
                _audio.PlayStatic(component.ScaresSound, uid, coords);
            });
            args.Handled = true;
        }
        private void OnDoorOpenAction(EntityUid uid, SCP173Component component, DoorOpenActionEvent args)
        {
            if (!component.Enabled || component.LookedAt) return;
            EntityUid openedDoor = EntityUid.Invalid;
            foreach (var door in _lookup.GetEntitiesInRange(uid, 1))
            {
                if (TryComp<DoorComponent>(door, out var doorComp) && doorComp.ClickOpen && doorComp.State == DoorState.Closed)
                    if (_doorSys.TryOpen(door, doorComp, quiet: true))
                        openedDoor = door;
            }
            if (openedDoor.IsValid())
            {
                _audio.PlayPvs(component.DoorOpenSound, openedDoor);
                args.Handled = true;
            }
        }
        #endregion

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
                Dirty(comp);
                if (isLookedAt) OnStartLookedAt(ent, comp);
                    else OnStopLookedAt(ent, comp);
            }
        }

        private void OnStartLookedAt(EntityUid uid, SCP173Component component)
        {
            var curtime = _gameTiming.CurTime;
            _actionSys.SetEnabled(component.BlindAction, true);
            _actionSys.AddAction(uid, component.BlindAction, uid);
            component.BlindAction.Cooldown = (curtime, curtime + component.BlindAction.UseDelay.GetValueOrDefault());

            if (TryComp<InputMoverComponent>(uid, out var input)) input.CanMove = false;
            _physics.SetFixedRotation(uid, true);
        }
        private void OnStopLookedAt(EntityUid uid, SCP173Component component)
        {
            _actionSys.SetEnabled(component.BlindAction, false);
            _actionSys.RemoveAction(uid, component.BlindAction);

            if (TryComp<InputMoverComponent>(uid, out var input)) input.CanMove = true;
            _physics.SetFixedRotation(uid, false);
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
                if ( IsValidObserver(entity) &&
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
        private bool IsValidObserver(EntityUid ent)
        {
            return HasComp<MindComponent>(ent) &&
                   //!HasComp<SCP173Component>(ent) &&
                   //!HasComp<GhostComponent>(ent) &&
                   _mobState.IsAlive(ent) &&
                   _tagSys.HasTag(ent, "EyeSight");
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
    public sealed class DoorOpenActionEvent : InstantActionEvent { }
}
