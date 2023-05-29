using System.Linq;
using Content.Server.Actions;
using Content.Server.NPC.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Flesh;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.Flesh
{
    public sealed class FleshWormSystem : EntitySystem
    {
        [Dependency] private SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedCombatModeSystem _combat = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly ActionsSystem _action = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<FleshWormComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<FleshWormComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<FleshWormComponent, ThrowDoHitEvent>(OnWormDoHit);
            SubscribeLocalEvent<FleshWormComponent, GotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<FleshWormComponent, GotUnequippedEvent>(OnGotUnequipped);
            SubscribeLocalEvent<FleshWormComponent, GotEquippedHandEvent>(OnGotEquippedHand);
            SubscribeLocalEvent<FleshWormComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<FleshWormComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<FleshWormComponent, FleshWormJumpActionEvent>(OnJumpWorm);
        }

        private void OnStartup(EntityUid uid, FleshWormComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionWormJump, null);
        }

        private void OnWormDoHit(EntityUid uid, FleshWormComponent component, ThrowDoHitEvent args)
        {
            if (component.IsDeath)
                return;
            if (HasComp<FleshCultistComponent>(args.Target))
                return;
            if (!HasComp<HumanoidAppearanceComponent>(args.Target))
                return;
            if (TryComp(args.Target, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState is not MobState.Alive)
                {
                    return;
                }
            }
            _inventory.TryGetSlotEntity(args.Target, "head", out var headItem);
            if (HasComp<IngestionBlockerComponent>(headItem))
                return;

            var equipped = _inventory.TryEquip(args.Target, uid, "mask", true);
            if (!equipped)
                return;

            component.EquipedOn = args.Target;

            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-hit-user"),
                args.Target, args.Target, PopupType.LargeCaution);

            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-hit-mob",
                    ("entity", args.Target)),
                uid, uid, PopupType.LargeCaution);

            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-eat-face-others",
                ("entity", args.Target)), args.Target, Filter.PvsExcept(uid), true, PopupType.Large);

            EntityManager.RemoveComponent<CombatModeComponent>(uid);
            _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _damageableSystem.TryChangeDamage(args.Target, component.Damage, origin: args.User);
        }

        private void OnGotEquipped(EntityUid uid, FleshWormComponent component, GotEquippedEvent args)
        {
            if (args.Slot != "mask")
                return;
            component.EquipedOn = args.Equipee;
            EntityManager.RemoveComponent<CombatModeComponent>(uid);
        }

        private void OnUnequipAttempt(EntityUid uid, FleshWormComponent component, BeingUnequippedAttemptEvent args)
        {
            if (args.Slot != "mask")
                return;
            if (component.EquipedOn != args.Unequipee)
                return;
            if (HasComp<FleshCultistComponent>(args.Unequipee))
                return;
            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-try-unequip"),
                args.Unequipee, args.Unequipee, PopupType.Large);
            args.Cancel();
        }

        private void OnGotEquippedHand(EntityUid uid, FleshWormComponent component, GotEquippedHandEvent args)
        {
            if (HasComp<FleshPudgeComponent>(args.User))
                return;
            if (HasComp<FleshCultistComponent>(args.User))
                return;
            if (component.IsDeath)
                return;
            // _handsSystem.TryDrop(args.User, uid, checkActionBlocker: false);
            _damageableSystem.TryChangeDamage(args.User, component.Damage);
            _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-bite-user"),
                args.User, args.User);
        }

        private void OnGotUnequipped(EntityUid uid, FleshWormComponent component, GotUnequippedEvent args)
        {
            if (args.Slot != "mask")
                return;
            component.EquipedOn = new EntityUid();
            var combatMode = EntityManager.AddComponent<CombatModeComponent>(uid);
            _combat.SetInCombatMode(uid, true, combatMode);
            EntityManager.AddComponent<NPCMeleeCombatComponent>(uid);
        }

        private void OnMeleeHit(EntityUid uid, FleshWormComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            foreach (var entity in args.HitEntities)
            {
                if (!HasComp<HumanoidAppearanceComponent>(entity))
                    return;

                if (TryComp(entity, out MobStateComponent? mobState))
                {
                    if (mobState.CurrentState is not MobState.Alive)
                    {
                        return;
                    }
                }

                _inventory.TryGetSlotEntity(entity, "head", out var headItem);
                if (HasComp<IngestionBlockerComponent>(headItem))
                    return;

                var random = new Random();
                var shouldEquip = random.Next(1, 101) <= FleshWormComponent.ChansePounce;
                if (!shouldEquip)
                    return;

                var equipped = _inventory.TryEquip(entity, uid, "mask", true);
                if (!equipped)
                    return;

                component.EquipedOn = entity;

                _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-hit-user"),
                    entity, entity, PopupType.LargeCaution);

                _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-hit-mob", ("entity", entity)),
                    uid, uid, PopupType.LargeCaution);

                _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-eat-face-others",
                    ("entity", entity)), entity, Filter.PvsExcept(entity), true, PopupType.Large);
                EntityManager.RemoveComponent<CombatModeComponent>(uid);
                _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
                _damageableSystem.TryChangeDamage(entity, component.Damage, origin: entity);
                break;
            }
        }

        private static void OnMobStateChanged(EntityUid uid, FleshWormComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                component.IsDeath = true;
            }
        }

        public sealed class FleshWormJumpActionEvent : WorldTargetActionEvent
        {

        };

        private void OnJumpWorm(EntityUid uid, FleshWormComponent component, FleshWormJumpActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var xform = Transform(uid);
            var mapCoords = args.Target.ToMap(EntityManager);
            Logger.Info(xform.MapPosition.ToString());
            Logger.Info(mapCoords.ToString());
            var direction = mapCoords.Position - xform.MapPosition.Position;
            Logger.Info(direction.ToString());

            _throwing.TryThrow(uid, direction, 7F, uid, 10F);
            if (component.SoundWormJump != null)
            {
                _audioSystem.PlayPvs(component.SoundWormJump, uid, component.SoundWormJump.Params);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<FleshWormComponent>())
            {
                comp.Accumulator += frameTime;

                if (comp.Accumulator <= comp.DamageFrequency)
                    continue;

                comp.Accumulator = 0;

                if (comp.EquipedOn is not { Valid: true } targetId)
                    continue;
                if (HasComp<FleshCultistComponent>(comp.EquipedOn))
                    return;
                if (TryComp(targetId, out MobStateComponent? mobState))
                {
                    if (mobState.CurrentState is not MobState.Alive)
                    {
                        _inventory.TryUnequip(targetId, "mask", true, true);
                        comp.EquipedOn = new EntityUid();
                        return;
                    }
                }
                _damageableSystem.TryChangeDamage(targetId, comp.Damage);
                _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-eat-face-user"),
                    targetId, targetId, PopupType.LargeCaution);
                _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-worm-eat-face-others",
                    ("entity", targetId)), targetId, Filter.PvsExcept(targetId), true);
            }
        }
    }
}
