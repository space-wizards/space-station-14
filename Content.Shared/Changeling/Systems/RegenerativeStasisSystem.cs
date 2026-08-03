using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Changeling.Systems;

public sealed partial class RegenerativeStasisSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobs = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedDeathgaspSystem _deathgasp = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<RegenerativeStasisActionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.InitialName = MetaData(ent).EntityName;
        ent.Comp.InitialDescription = MetaData(ent).EntityDescription;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<RegenerativeStasisActionComponent> ent, ref ActionRelayedEvent<MobStateChangedEvent> args)
    {
        // If we are revived cancel the stasis.
        if (args.Args.NewMobState == MobState.Alive && ent.Comp.IsInStasis)
            CancelStasis(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnMoveGhost(Entity<RegenerativeStasisActionComponent> ent, ref ActionRelayedEvent<GhostAttemptEvent> args)
    {
        if (ent.Comp.AllowGhosting || !ent.Comp.IsInStasis)
            return;

        args.Args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStasisUse(Entity<RegenerativeStasisActionComponent> ent, ref ChangelingStasisActionEvent args)
    {
        if (ent.Comp.IsInStasis)
        {
            ExitStasis((ent, ent.Comp), args.Performer);
            args.Handled = true; //Only handle when exiting, as we don't need the useDelay otherwise.
            return;
        }

        EnterStasis((ent, ent.Comp), args.Performer);
    }

    /// <summary>
    /// Enter the stasis and set the action cooldown depending on the damage you have taken.
    /// </summary>
    public void EnterStasis(Entity<RegenerativeStasisActionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.IsInStasis)
            return;

        // If going from Alive to Dead fake a death gasp.
        // If going from Critical to Dead then DeathGaspSystem is already doing this,
        // so we don't want to do it twice.
        if (_mobs.IsAlive(target))
            _deathgasp.Deathgasp(target);

        // Die temporarily until we revive.
        // Ghosting will be blocked while in stasis.
        if (!_mobs.IsDead(target))
            _mobs.ChangeMobState(target, MobState.Dead);

        _popup.PopupEntity(Loc.GetString("changeling-stasis-enter"), target, target, PopupType.MediumCaution);

        ent.Comp.IsInStasis = true;
        Dirty(ent);

        var stasisDuration = ent.Comp.MinStasisCooldown;

        stasisDuration += ent.Comp.BonusCooldownPerDamage * (double)_damage.GetTotalDamage(target);
        stasisDuration = new TimeSpan(Math.Clamp(stasisDuration.Ticks, ent.Comp.MinStasisCooldown.Ticks, ent.Comp.MaxStasisCooldown.Ticks)); // No clamp method for TimeSpans

        _metaData.SetEntityName(ent, Loc.GetString("changeling-stasis-active-name"));
        _metaData.SetEntityDescription(ent, Loc.GetString("changeling-stasis-active-desc"));

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
        _actions.SetCooldown(ent.Owner, stasisDuration);
    }

    /// <summary>
    /// Exit the stasis and heal all damage and bloodloss.
    /// TODO: Maybe add a some sort of rejuvenate lite so that we can also heal some status effects?
    /// </summary>
    public void ExitStasis(Entity<RegenerativeStasisActionComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        // Heal all damage.
        _damage.ClearAllDamage(target);

        // Heal bloodloss and stop bleeding.
        if (TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            _bloodstream.TryRegulateBloodLevel((target, bloodstream), bloodstream.BloodReferenceSolution.MaxVolume);
            _bloodstream.TryModifyBleedAmount((target, bloodstream), -bloodstream.BleedAmount);
        }

        // Revive.
        _mobs.ChangeMobState(target, MobState.Alive);

        _popup.PopupEntity(Loc.GetString("changeling-stasis-exit"), Loc.GetString("changeling-stasis-exit-others", ("user", target)), target, target, PopupType.MediumCaution);
        _audio.PlayPredicted(ent.Comp.ExitSound, target, target);

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
    }

    /// <summary>
    /// Cancel the stasis without healing.
    /// </summary>
    public void CancelStasis(Entity<RegenerativeStasisActionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.IsInStasis)
            return;

        ent.Comp.IsInStasis = false;
        Dirty(ent);

        if (ent.Comp.InitialName != null)
            _metaData.SetEntityName(ent, ent.Comp.InitialName);
        if (ent.Comp.InitialDescription != null)
            _metaData.SetEntityDescription(ent, ent.Comp.InitialDescription);

        _actions.SetToggled(ent.Owner, ent.Comp.IsInStasis);
    }
}

/// <summary>
/// Action event for entering/leaving the stasis.
/// </summary>
public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;
