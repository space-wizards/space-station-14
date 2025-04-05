using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Stack;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
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
        [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly TagSystem _tags = default!;
        [Dependency] private readonly StackSystem _stacks = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

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

            var ev = new RepairedEvent((uid, component), args.User);
            RaiseLocalEvent(uid, ref ev);
        }

        public void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!IsRepairable(uid, component.Damage))
            {
                return;
            }

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

            var usedRepairSpecifier = args.UsedRepairSpecifier;

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
                if (!_stacks.Use(usedMaterial, usedRepairSpecifier.MaterialCost, stackComp))
                {
                    return;
                }
                actualMaterialCost = usedRepairSpecifier.MaterialCost;
            }
            else
            {
                _entities.DeleteEntity(usedMaterial);
            }

            if (usedRepairSpecifier.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, usedRepairSpecifier.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()} using {actualMaterialCost} {usedMaterialName}");
            }
            else if (usedRepairSpecifier.RepairProportion != null)
            {
                if (!TryComp(uid, out DamageableComponent? damageComp))
                {
                    return;
                }

                var maxHeal = usedRepairSpecifier.RepairProportion.Value * _destructibleSystem.DestroyedAt(uid);
                var totalDamage = damageComp.TotalDamage;

                var newDamage = new DamageSpecifier();
                foreach (var (damageName, damage) in damageComp.Damage.DamageDict)
                {
                    var damageProportion = damage / totalDamage;
                    newDamage.DamageDict[damageName] = damageProportion * maxHeal * -1;
                }
                var damageChanged = _damageableSystem.TryChangeDamage(uid, newDamage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()} using {actualMaterialCost} {usedMaterialName}");
            }
            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health using {actualMaterialCost} {usedMaterialName}");
            }

            if (stackComp is not null)
            {
                if (usedRepairSpecifier.AutoRepeat &&
                    args.Used is EntityUid && args.Target is EntityUid)
                {
                    var ev = new InteractUsingEvent(args.User, args.Used.Value, args.Target.Value, new());
                    OnRepair(uid, component, ev);
                }
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
            _stacks.GetCount(usedMaterial, stackComp) < args.Event.UsedRepairSpecifier.MaterialCost)
            {
                args.Cancel();
            }
        }

        private void OnRepair(EntityUid uid, RepairableByReplacementComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Only try repair if the used entity has the right tag
            RepairMaterialSpecifier? usedRepairSpecifier = null;
            foreach (var (repairType, repairSpecifier) in component.RepairTypes)
            {
                if (_tags.HasTag(args.Used, repairType))
                {
                    usedRepairSpecifier = repairSpecifier;
                    break;
                }
            }

            if (usedRepairSpecifier is null)
            {
                return;
            }

            if (!IsRepairable(uid, usedRepairSpecifier.Damage))
            {
                return;
            }

            // Do not attempt to repair with a stacked entity if the stack does not have enough materialss
            if (TryComp(args.Used, out StackComponent? stackComp) &&
               _stacks.GetCount(args.Used, stackComp) < usedRepairSpecifier.MaterialCost)
            {
                return;
            }

            float delay = usedRepairSpecifier.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
            {
                if (!usedRepairSpecifier.AllowSelfRepair)
                    return;

                delay *= usedRepairSpecifier.SelfRepairPenalty;
            }

            var ev = new RepairByReplacementFinishedEvent(usedRepairSpecifier);
            var doAfterArgs = new DoAfterArgs(_entities, args.User, delay, ev, uid, uid, args.Used)
            {
                BreakOnHandChange = true,
                NeedHand = true
            };

            if (stackComp is not null)
            {
                doAfterArgs.AttemptFrequency = AttemptFrequency.EveryTick;
            }

            args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
        }

        /// <summary>
        /// Checks if the entity can be repaired by the damageRepair.
        /// damageRepair must have negative values to repair.
        /// If damageRepair does not repair the damage of the entity, returns false.
        /// If damageRepair is null, it is assumed that the repair mechanism will heal ALL damage
        /// </summary>
        private bool IsRepairable(EntityUid uid, DamageSpecifier? damageRepair)
        {
            // Only try repair the target if it is damaged
            if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
                return false;

            if (damageRepair is null)
            {
                return true;
            }

            // Only try repair the target if it's damage can be repaired by this mechanism
            var repairByGroup = damageRepair.DamageDict;

            foreach (var (damageType, damageValue) in damageable.Damage.DamageDict)
            {
                if (!repairByGroup.TryGetValue(damageType, out FixedPoint2 val) ||
                    val >= 0)
                {
                    continue;
                }
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Event raised on an entity when its successfully repaired.
    /// </summary>
    /// <param name="Ent"></param>
    /// <param name="User"></param>
    [ByRefEvent]
    public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

}
