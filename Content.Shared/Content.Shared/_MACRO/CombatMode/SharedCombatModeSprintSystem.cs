using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared._MACRO.CombatMode;

public abstract partial class SharedCombatModeSprintSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private DamageOnHighSpeedImpactSystem _impact = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// Last combat-mode state we applied impact collide settings for (avoids redundant Dirty).
    /// </summary>
    private readonly Dictionary<EntityUid, bool> _lastImpactCollideCombat = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeSprintComponent, ToggleCombatActionEvent>(
            OnCombatToggle,
            after: [typeof(SharedCombatModeSystem)]);
        SubscribeLocalEvent<CombatModeSprintComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<CombatModeSprintComponent, ComponentShutdown>(OnSprintShutdown);
    }

    private void OnSprintShutdown(Entity<CombatModeSprintComponent> ent, ref ComponentShutdown args)
    {
        _lastImpactCollideCombat.Remove(ent.Owner);
    }

    private void OnCombatToggle(Entity<CombatModeSprintComponent> ent, ref ToggleCombatActionEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
        if (ent.Comp.BeginCombatMessage != null && _combatMode.IsInCombatMode(ent))
            _popup.PopupEntity(Loc.GetString(ent.Comp.BeginCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
        if (ent.Comp.EndCombatMessage != null && !_combatMode.IsInCombatMode(ent))
            _popup.PopupEntity(Loc.GetString(ent.Comp.EndCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
    }

    private void OnRefreshMovespeed(Entity<CombatModeSprintComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var inCombat = _combatMode.IsInCombatMode(ent);

        if (inCombat)
            args.ModifySpeed(ent.Comp.SprintCoefficient);
        else
            args.ModifySpeed(1f);

        if (!ent.Comp.DoImpactDamage)
            return;

        if (_lastImpactCollideCombat.TryGetValue(ent.Owner, out var last) && last == inCombat)
            return;

        if (inCombat)
            _impact.ChangeCollide(ent, ent.Comp.MinimumSpeed, ent.Comp.StunSeconds, ent.Comp.DamageCooldown, ent.Comp.SpeedDamage);
        else
            _impact.ChangeCollide(ent, ent.Comp.DefaultMinimumSpeed, ent.Comp.DefaultStunSeconds, ent.Comp.DefaultDamageCooldown, ent.Comp.DefaultSpeedDamage);

        _lastImpactCollideCombat[ent.Owner] = inCombat;
    }
}
