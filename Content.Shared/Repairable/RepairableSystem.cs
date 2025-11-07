using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public sealed partial class RepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<RepairableComponent, RepairFinishedEvent>(OnRepairFinished);
    }

    private void OnRepairFinished(Entity<RepairableComponent> ent,  ref RepairFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent.Owner, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
            return;

        if (ent.Comp.Damage != null)
        {
            var damageChanged = _damageableSystem.ChangeDamage(ent.Owner, ent.Comp.Damage, true, false, origin: args.User);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");
        }

        else
        {
            // Repair all damage
            _damageableSystem.SetAllDamage((ent.Owner, damageable), 0);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
        }

        var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
        _popup.PopupClient(str, ent.Owner, args.User);

        var ev = new RepairedEvent(ent, args.User);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void Repair(Entity<RepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Only try repair the target if it is damaged
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable) || damageable.TotalDamage == 0)
            return;

        float delay = ent.Comp.DoAfterDelay;

        // Add a penalty to how long it takes if the user is repairing itself
        if (args.User == args.Target)
        {
            if (!ent.Comp.AllowSelfRepair)
                return;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        // Run the repairing doafter
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new RepairFinishedEvent(), ent.Comp.FuelCost);
    }
}

/// <summary>
/// Event raised on an entity when its successfully repaired.
/// </summary>
/// <param name="Ent"></param>
/// <param name="User"></param>
[ByRefEvent]
public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

[Serializable, NetSerializable]
public sealed partial class RepairFinishedEvent : SimpleDoAfterEvent;
