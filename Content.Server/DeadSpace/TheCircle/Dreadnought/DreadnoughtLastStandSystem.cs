// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.TheCircle.Dreadnought;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.TheCircle.Dreadnought;

public sealed class DreadnoughtLastStandSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DreadnoughtLastStandComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<DreadnoughtLastStandComponent, DreadnoughtLastStandActionEvent>(OnAction);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, UpdateMobStateEvent>(OnUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, DamageChangedEvent>(OnDamageChanged,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, BeforeAlertSeverityCheckEvent>(OnBeforeAlertSeverity);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnGetActions(Entity<DreadnoughtLastStandComponent> ent, ref GetItemActionsEvent args)
    {
        if (!ent.Comp.Used &&
            args.SlotFlags is { } slotFlags &&
            (slotFlags & ent.Comp.RequiredSlots) != SlotFlags.NONE)
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnAction(Entity<DreadnoughtLastStandComponent> ent, ref DreadnoughtLastStandActionEvent args)
    {
        if (args.Handled ||
            ent.Comp.Used ||
            HasComp<DreadnoughtLastStandActiveComponent>(args.Performer) ||
            !TryComp<ClothingComponent>(ent, out var clothing) ||
            clothing.InSlotFlag is not { } slotFlags ||
            (slotFlags & ent.Comp.RequiredSlots) == SlotFlags.NONE ||
            Transform(ent).ParentUid != args.Performer)
            return;

        args.Handled = true;
        ent.Comp.Used = true;
        var active = EnsureComp<DreadnoughtLastStandActiveComponent>(args.Performer);
        active.EndsAt = _timing.CurTime + ent.Comp.Duration;
        active.SpeedModifier = ent.Comp.SpeedModifier;
        active.DeathDamage = ent.Comp.DeathDamage;
        active.Expired = false;
        if (!HasComp<IgnoreSlowOnDamageComponent>(args.Performer))
        {
            AddComp<IgnoreSlowOnDamageComponent>(args.Performer);
            active.AppliedIgnoreSlowOnDamage = true;
        }
        EnsureComp<UnremoveableComponent>(ent);
        Dirty(args.Performer, active);
        _actions.RemoveAction(args.Performer, ent.Comp.ActionEntity);
        _movement.RefreshMovementSpeedModifiers(args.Performer);
        _mobState.UpdateMobState(args.Performer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<DreadnoughtLastStandActiveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Expired || _timing.CurTime < component.EndsAt)
                continue;

            component.Expired = true;
            Dirty(uid, component);
            _mobState.UpdateMobState(uid);
        }
    }

    private void OnUpdateMobState(Entity<DreadnoughtLastStandActiveComponent> ent, ref UpdateMobStateEvent args)
    {
        if (ent.Comp.Expired ||
            TryComp<DamageableComponent>(ent, out var damageable) && damageable.TotalDamage >= ent.Comp.DeathDamage)
        {
            args.State = MobState.Dead;
            return;
        }

        args.State = MobState.Alive;
    }

    private void OnDamageChanged(Entity<DreadnoughtLastStandActiveComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Expired || args.Damageable.TotalDamage >= ent.Comp.DeathDamage)
            return;

        _thresholds.SetCurrentThresholdState(ent, MobState.Alive);
        _mobState.ChangeMobState(ent, MobState.Alive);
    }

    private void OnBeforeAlertSeverity(Entity<DreadnoughtLastStandActiveComponent> ent,
        ref BeforeAlertSeverityCheckEvent args)
    {
        if (ent.Comp.Expired)
            return;

        args.CancelUpdate = true;
    }

    private void OnExamined(Entity<DreadnoughtLastStandActiveComponent> ent, ref ExaminedEvent args)
    {
        var remaining = TimeSpan.FromTicks(Math.Max(0, (ent.Comp.EndsAt - _timing.CurTime).Ticks));
        args.PushMarkup(Loc.GetString("dreadnought-last-stand-examine",
            ("time", remaining.ToString(@"mm\:ss"))));
    }

    private void OnRefreshSpeed(Entity<DreadnoughtLastStandActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnActiveShutdown(Entity<DreadnoughtLastStandActiveComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AppliedIgnoreSlowOnDamage)
            RemComp<IgnoreSlowOnDamageComponent>(ent);

        _movement.RefreshMovementSpeedModifiers(ent);
    }
}
