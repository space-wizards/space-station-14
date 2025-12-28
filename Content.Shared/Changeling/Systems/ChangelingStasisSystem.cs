using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
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
        Dirty(ent);
    }

    private void OnShutdown(Entity<ChangelingStasisComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.RegenStasisActionEntity);
    }

    private void OnStateChanged(Entity<ChangelingStasisComponent> ent, ref MobStateChangedEvent args)
    {
        // If we are revived cancel the stasis.
        if (args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis(ent.AsNullable());
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

    /// <summary>
    /// Enter the stasis and set the action cooldown depending on the damage you have taken.
    /// </summary>
    public void EnterStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.IsInStasis)
            return;

        // If going from Alive to Dead fake a death gasp.
        // If going from Critical to Dead then DeathGaspSystem is already doing this,
        // so we don't want to do it twice.
        if (_mobs.IsAlive(ent))
            _deathgasp.Deathgasp(ent);

        // Die temporarily until we revive.
        // Ghosting will be blocked while in stasis.
        if (!_mobs.IsDead(ent))
            _mobs.ChangeMobState(ent.Owner, MobState.Dead);

        _popup.PopupClient(Loc.GetString("changeling-stasis-enter"), ent.Owner, ent.Owner, PopupType.MediumCaution);

        ent.Comp.IsInStasis = true;
        Dirty(ent);

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        var stasisDuration = ent.Comp.MinStasisCooldown;

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable))
        {
            stasisDuration += ent.Comp.BonusCooldownPerDamage * (double)damageable.TotalDamage;
            stasisDuration = new TimeSpan(Math.Clamp(stasisDuration.Ticks, ent.Comp.MinStasisCooldown.Ticks, ent.Comp.MaxStasisCooldown.Ticks)); // No clamp method for TimeSpans
        }

        _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, Loc.GetString("changeling-stasis-active-name"));
        _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, Loc.GetString("changeling-stasis-active-desc"));

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);
        _actions.SetCooldown(ent.Comp.RegenStasisActionEntity, stasisDuration);
    }

    /// <summary>
    /// Exit the stasis and heal all damage and bloodloss.
    /// TODO: Maybe add a some sort of rejuvenate lite so that we can also heal some status effects?
    /// </summary>
    public void ExitStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        // Heal all damage.
        _damage.ClearAllDamage(ent.Owner);

        // Heal bloodloss and stop bleeding.
        if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            _bloodstream.TryModifyBloodLevel((ent, bloodstream), bloodstream.BloodMaxVolume);
            _bloodstream.TryModifyBleedAmount((ent, bloodstream), -bloodstream.BleedAmount);
        }

        // Revive.
        _mobs.ChangeMobState(ent.Owner, MobState.Alive);

        _popup.PopupPredicted(Loc.GetString("changeling-stasis-exit"), Loc.GetString("changeling-stasis-exit-others", ("user", ent.Owner)), ent.Owner, ent.Owner, PopupType.MediumCaution);
        _audio.PlayPredicted(ent.Comp.ExitSound, ent.Owner, ent.Owner);

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);
    }

    /// <summary>
    /// Cancel the stasis without healing.
    /// </summary>
    public void CancelStasis(Entity<ChangelingStasisComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.RegenStasisActionEntity == null)
            return;

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent.Comp.RegenStasisActionEntity.Value, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Comp.RegenStasisActionEntity, ent.Comp.IsInStasis);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
