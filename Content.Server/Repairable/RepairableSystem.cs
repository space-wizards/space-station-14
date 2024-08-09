using Content.Server.Administration.Logs;
using Content.Server.Stack;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Repairable
{
    public sealed class RepairableSystem : SharedRepairableSystem
    {
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly TagSystem _tags = default!;
        [Dependency] private readonly StackSystem _stacks = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
            SubscribeLocalEvent<RepairableComponent, RepairFinishedEvent>(OnRepairFinished);

            SubscribeLocalEvent<RepairableByReplacementComponent, InteractUsingEvent>(OnRepair);
            SubscribeLocalEvent<RepairableByReplacementComponent, DoAfterAttemptEvent<RepairByReplacementFinishedEvent>>(DuringRepair);
            SubscribeLocalEvent<RepairableByReplacementComponent, RepairByReplacementFinishedEvent>(OnRepairFinished);
        }


        private void OnRepairFinished(EntityUid uid, RepairableComponent component, RepairFinishedEvent args)
        {
            if (args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            if (component.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()}");
            }

            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health");
            }

            var str = Loc.GetString("comp-repairable-repair",
                ("target", uid),
                ("tool", args.Used!));
            _popup.PopupEntity(str, uid, args.User);
        }

        public void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Only try repair the target if it is damaged
            if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
                return;

            float delay = component.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
            {
                if (!component.AllowSelfRepair)
                    return;

                delay *= component.SelfRepairPenalty;
            }

            // Run the repairing doafter
            args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, delay, component.QualityNeeded, new RepairFinishedEvent(), component.FuelCost);
        }

        private void OnRepairFinished(EntityUid uid, RepairableByReplacementComponent component, RepairByReplacementFinishedEvent args)
        {
            if (args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            if (args.Used is not EntityUid usedMaterial ||
                Prototype(usedMaterial) is not EntityPrototype proto)
            {
                return;
            }

            var actualMaterialCost = 1;
            var usedMaterialName = proto.Name;

            if (TryComp(args.Used, out StackComponent? stackComp))
            {
                if (!_stacks.Use(usedMaterial, component.MaterialCost, stackComp))
                {
                    return;
                }
                actualMaterialCost = component.MaterialCost;
            }
            else
            {
                _entities.DeleteEntity(usedMaterial);
            }

            if (component.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()} using {actualMaterialCost} {usedMaterialName}");
            }

            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health using {actualMaterialCost} {usedMaterialName}");
            }

            var str = Loc.GetString("comp-repairable-replacement-repair",
                ("target", uid),
                ("amountUsed", actualMaterialCost),
                ("material", args.Used!));
            _popup.PopupEntity(str, uid, args.User);
        }

        private void DuringRepair(EntityUid uid, RepairableByReplacementComponent component, DoAfterAttemptEvent<RepairByReplacementFinishedEvent> args)
        {
            // Cancel if there is the stack is too small 
            if (args.Event.Used is EntityUid usedMaterial &&
            TryComp(args.Event.Used!, out StackComponent? stackComp) &&
            _stacks.GetCount(usedMaterial, stackComp) < component.MaterialCost)
            {
                args.Cancel();
            }
        }

        private void OnRepair(EntityUid uid, RepairableByReplacementComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Do not attempt to repair with a stacked entity if the stack does not have enough materialss
            if (TryComp(args.Used, out StackComponent? stackComp) &&
               _stacks.GetCount(args.Used, stackComp) < component.MaterialCost)
            {
                return;
            }

            // Only try repair if the used entity has the right tag
            if (!_tags.HasTag(args.Used, component.RepairType))
            {
                return;
            }

            // Only try repair the target if it is damaged
            if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
                return;

            float delay = component.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
            {
                if (!component.AllowSelfRepair)
                    return;

                delay *= component.SelfRepairPenalty;
            }

            var ev = new RepairByReplacementFinishedEvent();
            var doAfterArgs = new DoAfterArgs(_entities, args.User, delay, ev, uid, uid, args.Used)
            {
                BreakOnHandChange = true
            };

            if (stackComp is not null)
            {
                doAfterArgs.AttemptFrequency = AttemptFrequency.EveryTick;
            }

            args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
        }
    }
}
