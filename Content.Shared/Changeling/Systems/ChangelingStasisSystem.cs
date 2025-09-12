using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RegenerativeStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RegenerativeStasisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RegenerativeStasisComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<RegenerativeStasisComponent, ChangelingStasisActionEvent>(OnStasisUse);
    }

    private void OnMapInit(Entity<RegenerativeStasisComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.RegenStasisActionEntity, ent.Comp.RegenStasisAction);
    }

    private void OnShutdown(Entity<RegenerativeStasisComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.RegenStasisActionEntity != null)
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.RegenStasisActionEntity);
        }
    }

    private void OnStateChanged(Entity<RegenerativeStasisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis(ent);

        // We force enter stasis on death. This is mostly for convenience as we do not have biomass or chemicals yet.
        // TODO: Remove once we have biomass/chemicals or some other requirement.
        if (args.NewMobState == MobState.Dead && !ent.Comp.IsInStasis)
            EnterStasis(ent);
    }

    private void OnStasisUse(Entity<RegenerativeStasisComponent> ent, ref ChangelingStasisActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

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

        _mobs.ChangeMobState(ent.Owner, MobState.Dead);

        ent.Comp.IsInStasis = true;

        _actions.SetCooldown(ent.Comp.RegenStasisActionEntity, ent.Comp.StasisCooldown);
    }

    private void ExitStasis(Entity<RegenerativeStasisComponent> ent)
    {
        if (!ent.Comp.IsInStasis)
            return;

        _mobs.ChangeMobState(ent.Owner, MobState.Alive);

        ent.Comp.IsInStasis = false;

        // We remove all the damage.
        _damage.SetAllDamage(ent.Owner, 0);
    }

    private void CancelStasis(Entity<RegenerativeStasisComponent> ent)
    {
        ent.Comp.IsInStasis = false;
        _actions.RemoveCooldown(ent.Comp.RegenStasisActionEntity);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
