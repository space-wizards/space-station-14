using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RegenerativeStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RegenerativeStasisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RegenerativeStasisComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<RegenerativeStasisComponent, ChangelingStasisActionEvent>(OnStasisUse);

        SubscribeLocalEvent<RegenerativeStasisComponent, EntityGhostAttemptEvent>(OnMoveGhost);
    }

    private void OnMapInit(Entity<RegenerativeStasisComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.RegenStasisActionEntity, ent.Comp.RegenStasisAction);
    }

    private void OnShutdown(Entity<RegenerativeStasisComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.RegenStasisActionEntity);
    }

    private void OnStateChanged(Entity<RegenerativeStasisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis(ent);
    }

    private void OnMoveGhost(Entity<RegenerativeStasisComponent> ent, ref EntityGhostAttemptEvent args)
    {
        if (ent.Comp.AllowGhosting || !ent.Comp.IsInStasis)
            return;

        args.Cancel();
    }

    private void OnStasisUse(Entity<RegenerativeStasisComponent> ent, ref ChangelingStasisActionEvent args)
    {
        if (ent.Comp.IsInStasis)
        {
            ExitStasis(ent);
            return;
        }

        EnterStasis(ent);
    }

    private void EnterStasis(Entity<RegenerativeStasisComponent> ent)
    {
        if (ent.Comp.IsInStasis)
            return;

        if (!_mobs.IsDead(ent))
            _mobs.ChangeMobState(ent.Owner, MobState.Dead);

        ent.Comp.IsInStasis = true;

        Dirty(ent);

        _actions.SetCooldown(ent.Comp.RegenStasisActionEntity, ent.Comp.StasisCooldown);
    }

    private void ExitStasis(Entity<RegenerativeStasisComponent> ent)
    {
        if (!ent.Comp.IsInStasis)
            return;

        // We remove all the damage.
        _damage.SetAllDamage(ent.Owner, 0);

        _mobs.ChangeMobState(ent.Owner, MobState.Alive);
        _mobs.UpdateMobState(ent.Owner);

        ent.Comp.IsInStasis = false;

        Dirty(ent);
    }

    private void CancelStasis(Entity<RegenerativeStasisComponent> ent)
    {
        ent.Comp.IsInStasis = false;
        Dirty(ent);
        _actions.RemoveCooldown(ent.Comp.RegenStasisActionEntity);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
