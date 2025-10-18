﻿using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedDeathgaspSystem _deathgasp = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingStasisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingStasisComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<ChangelingStasisComponent, ChangelingStasisActionEvent>(OnStasisUse);

        SubscribeLocalEvent<ChangelingStasisComponent, GhostAttemptEvent>(OnMoveGhost);
    }

    private void OnMapInit(Entity<ChangelingStasisComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.RegenStasisActionEntity, ent.Comp.RegenStasisAction);

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        ent.Comp.InitialName = MetaData(ent.Comp.RegenStasisActionEntity.Value).EntityName;
        ent.Comp.InitialDescription = MetaData(ent.Comp.RegenStasisActionEntity.Value).EntityDescription;
    }

    private void OnShutdown(Entity<ChangelingStasisComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.RegenStasisActionEntity);
    }

    private void OnStateChanged(Entity<ChangelingStasisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis((ent, ent.Comp));
    }

    private void OnMoveGhost(Entity<ChangelingStasisComponent> ent, ref GhostAttemptEvent args)
    {
        if (ent.Comp.AllowGhosting || !ent.Comp.IsInStasis)
            return;

        args.Cancelled = true;
    }

    private void OnStasisUse(Entity<ChangelingStasisComponent> ent, ref ChangelingStasisActionEvent args)
    {
        if (ent.Comp.IsInStasis)
        {
            ExitStasis((ent, ent.Comp));
            args.Handled = true; //Only handle when exiting, as we don't need the useDelay otherwise.
            return;
        }

        EnterStasis((ent, ent.Comp));
    }

    public void EnterStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        if (ent.Comp.IsInStasis)
            return;

        if (!_mobs.IsDead(ent))
        {
            _deathgasp.Deathgasp(ent);
            _mobs.ChangeMobState(ent.Owner, MobState.Dead);
        }

        _popup.PopupClient(Loc.GetString("changeling-stasis-enter"), ent.Owner, ent.Owner, PopupType.MediumCaution);

        ent.Comp.IsInStasis = true;

        _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, Loc.GetString("changeling-stasis-active-name"));
        _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, Loc.GetString("changeling-stasis-active-desc"));

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);

        Dirty(ent);

        var stasisDuration = ent.Comp.StasisCooldown;

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable))
        {
            var damagePercentage = Math.Clamp((damageable.TotalDamage / ent.Comp.StasisDamageDelta).Float(), 0, 1);

            stasisDuration += ent.Comp.BonusStasisCooldown * damagePercentage;
        }

        _actions.SetCooldown(ent.Comp.RegenStasisActionEntity, stasisDuration);
    }

    public void ExitStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        if (!ent.Comp.IsInStasis)
            return;

        // We remove all the damage.
        _damage.SetAllDamage(ent.Owner, 0);

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            _bloodstream.TryModifyBloodLevel((ent, bloodstream), bloodstream.BloodMaxVolume);
            _bloodstream.TryModifyBleedAmount((ent, bloodstream), -bloodstream.BleedAmount);
        }

        _mobs.ChangeMobState(ent.Owner, MobState.Alive);

        ent.Comp.IsInStasis = false;

        _popup.PopupPredicted(Loc.GetString("changeling-stasis-exit"), Loc.GetString("changeling-stasis-exit-others", ("user", ent.Owner)), ent.Owner, ent.Owner, PopupType.MediumCaution);
        _audio.PlayPredicted(ent.Comp.ExitSound, ent.Owner, ent.Owner);

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);

        Dirty(ent);
    }

    public void CancelStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.RegenStasisActionEntity == null || !ent.Comp.IsInStasis)
            return;

        ent.Comp.IsInStasis = false;

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);

        Dirty(ent);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
