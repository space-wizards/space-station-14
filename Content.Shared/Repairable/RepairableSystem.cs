using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public sealed partial class RepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<RepairableComponent, RepairDoAfterEvent>(OnRepairDoAfter);
    }

    private void OnRepairDoAfter(Entity<RepairableComponent> ent, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent.Owner, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
            return;

        if (HasComp<MobStateComponent>(ent) && !_mobState.IsAlive(ent))
        {
            // the mob is crit or dead

            if (ent.Comp.DamageCrit != null) RepairSomeDamage(ent, ent.Comp.DamageCrit, args.User);
            else if (ent.Comp.Damage != null) RepairSomeDamage(ent, ent.Comp.Damage, args.User);
            else RepairAllDamage(ent, damageable, args.User);
        }
        else
        {
            // entity is alive or doesn't even have MobStateComponent

            if (ent.Comp.Damage != null) RepairSomeDamage(ent, ent.Comp.Damage, args.User);
            else RepairAllDamage(ent, damageable, args.User);
        }

        args.Repeat = ent.Comp.AutoDoAfter && damageable.TotalDamage > 0;
        args.Handled = true;

        if (args.Repeat)
        {
            var delay = ent.Comp.DoAfterDelay;
            _toolSystem.UseTool(args.Used!.Value, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new RepairDoAfterEvent(), ent.Comp.FuelCost);
        }
        else
        {
            var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
            _popup.PopupClient(str, ent.Owner, args.User);

            var ev = new RepairedEvent(ent, args.User);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    private void RepairSomeDamage(Entity<RepairableComponent> ent, Damage.DamageSpecifier damageAmount, EntityUid user)
    {
        var damageChanged = _damageableSystem.ChangeDamage(ent.Owner, damageAmount, true, false, origin: user);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} by {damageChanged.GetTotal()}");
    }

    private void RepairAllDamage(Entity<RepairableComponent> ent, DamageableComponent? damageable, EntityUid user)
    {
        _damageableSystem.SetAllDamage((ent.Owner, damageable), 0);
        _adminLogger.Add(LogType.Healed, $"{ToPrettyString(user):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
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
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new RepairDoAfterEvent(), ent.Comp.FuelCost);
    }
}

/// <summary>
/// Event raised on an entity when its successfully repaired.
/// </summary>
/// <param name="Ent"></param>
/// <param name="User"></param>
[ByRefEvent]
public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

/// <summary>
/// Do after event started when you try to fix a entity with RepairableComponent.
/// This doafter is started again if the entity has <see cref="AutoDoAfter"> set to true and not all damage was fixed yet.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RepairDoAfterEvent : SimpleDoAfterEvent;
