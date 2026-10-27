using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Cloning.Events;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Eye.Blinking;

/// <summary>
/// A system to control entities blinking their eyes.
/// </summary>
public abstract partial class EyeBlinkingSystem : EntitySystem
{
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    [Dependency] protected EntityQuery<EyeBlinkingComponent> EyeBlinkingQuery;
    [Dependency] protected EntityQuery<VisualOrganComponent> VisualOrganQuery;
    [Dependency] private EntityQuery<OrganComponent> _organQuery;

    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<EyeBlinkingComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.EyeToggleActionEntity == null)
            return;

        _actions.RemoveAction(ent.Comp.EyeToggleActionEntity);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnSleepStateChanged(Entity<EyeBlinkingComponent> ent, ref SleepStateChangedEvent args)
    {
        SetStatusFlag(ent, BlinkStatus.Sleeping, args.FellAsleep);
    }

    [SubscribeLocalEvent]
    private void OnSleepStateChanged(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<SleepStateChangedEvent> args)
    {
        var sleepArgs = args.Args;
        OnSleepStateChanged(ent, ref sleepArgs);
        args.Args = sleepArgs;
    }

    [SubscribeLocalEvent]
    private void OnApplyOrganProfileData(Entity<EyeBlinkingComponent> ent, ref ApplyOrganProfileDataEvent args)
    {
        SetEyelidsColor(ent, args.Base?.SkinColor);
        AddEyeAction(ent, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnApplyOrganProfileData(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<ApplyOrganProfileDataEvent> args)
    {
        if (args.Args.Base != null)
            SetEyelidsColor(ent, args.Args.Base.Value.SkinColor);
        else if (args.Args.Profiles?.FirstOrDefault() is { } profile)
            SetEyelidsColor(ent, profile.Value.SkinColor);

        AddEyeAction(ent, args.Body.Owner);
    }

    [SubscribeLocalEvent]
    private void OnOrganCopyAppearance(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<OrganCopyAppearanceEvent> args)
    {
        if (!VisualOrganQuery.HasComp(args.Args.Organ)
            || !EyeBlinkingQuery.TryComp(args.Args.Organ, out var cloneEyes))
            return;

        ent.Comp.EyeToggleAction = cloneEyes.EyeToggleAction;

        ent.Comp.EyelidsSprite = cloneEyes.EyelidsSprite;
        ent.Comp.EyelidsColor = cloneEyes.EyelidsColor;

        ent.Comp.MaxAsyncBlink = cloneEyes.MaxAsyncBlink;
        ent.Comp.MaxAsyncOpenBlink = cloneEyes.MaxAsyncOpenBlink;

        ent.Comp.MaxBlinkDuration = cloneEyes.MaxBlinkDuration;
        ent.Comp.MinBlinkDuration = cloneEyes.MinBlinkDuration;
        ent.Comp.MaxBlinkInterval = cloneEyes.MaxBlinkInterval;
        ent.Comp.MinBlinkInterval = cloneEyes.MinBlinkInterval;

        ent.Comp.BlinkSkinColorMultiplier = cloneEyes.BlinkSkinColorMultiplier;
        ent.Comp.BlinkEmoteId = [.. cloneEyes.BlinkEmoteId];
        ent.Comp.StatePrefix = cloneEyes.StatePrefix;

        Dirty(ent);

        AddEyeAction(ent, args.Body.Owner);
    }


    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<EyeBlinkingComponent> ent, ref MobStateChangedEvent args)
    {
        SetStatusFlag(ent, BlinkStatus.Dead, args.NewMobState == MobState.Dead);

        // Handle blink action if entity is dead or not.
        if (args.NewMobState == MobState.Dead)
        {
            if (ent.Comp.EyeToggleActionEntity == null)
                return;

            _actions.RemoveAction(args.Target, ent.Comp.EyeToggleActionEntity);
            ent.Comp.EyeToggleActionEntity = null;
            Dirty(ent);
        }
        else
            AddEyeAction(ent, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<MobStateChangedEvent> args)
    {
        var mobStateArgs = args.Args;
        OnMobStateChanged(ent, ref mobStateArgs);
        args.Args = mobStateArgs;
    }

    [SubscribeLocalEvent]
    private void OnBlindnessChanged(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        if (ent.Comp.EyeToggleActionEntity != null)
            _actions.SetToggled(ent.Comp.EyeToggleActionEntity, args.Blind);

        SetStatusFlag(ent, BlinkStatus.Blind, args.Blind);
    }

    [SubscribeLocalEvent]
    private void OnBlindnessChanged(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<BlindnessChangedEvent> args)
    {
        var blindnessArgs = args.Args;
        OnBlindnessChanged(ent, ref blindnessArgs);
        args.Args = blindnessArgs;
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<EyeBlinkingComponent> ent, ref ToggleEyesActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        SetStatusFlag(ent, BlinkStatus.EyesClosed, (ent.Comp.Status & BlinkStatus.EyesClosed) == BlinkStatus.Normal);

        _blindable.UpdateIsBlind(args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<ToggleEyesActionEvent> args)
    {
        var toggleArgs = args.Args;
        OnToggleAction(ent, ref toggleArgs);
        args.Args = toggleArgs;
    }

    [SubscribeLocalEvent]
    private void OnTrySee(Entity<EyeBlinkingComponent> ent, ref CanSeeAttemptEvent args)
    {
        // Only considering the close eyes action for this.
        // Permanent blindness handled elsewhere.
        if ((ent.Comp.Status & BlinkStatus.EyesClosed) != BlinkStatus.Normal)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnTrySee(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<CanSeeAttemptEvent> args)
    {
        var seeArgs = args.Args;
        OnTrySee(ent, ref seeArgs);
        args.Args = seeArgs;
    }

    [SubscribeLocalEvent]
    private void OnCloning(Entity<EyeBlinkingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<EyeBlinkingComponent>();
        cloneComp.EyeToggleAction = ent.Comp.EyeToggleAction;

        cloneComp.EyelidsSprite = ent.Comp.EyelidsSprite;
        cloneComp.EyelidsColor = ent.Comp.EyelidsColor;

        cloneComp.MaxAsyncBlink = ent.Comp.MaxAsyncBlink;
        cloneComp.MaxAsyncOpenBlink = ent.Comp.MaxAsyncOpenBlink;

        cloneComp.MaxBlinkDuration = ent.Comp.MaxBlinkDuration;
        cloneComp.MinBlinkDuration = ent.Comp.MinBlinkDuration;
        cloneComp.MaxBlinkInterval = ent.Comp.MaxBlinkInterval;
        cloneComp.MinBlinkInterval = ent.Comp.MinBlinkInterval;

        cloneComp.BlinkSkinColorMultiplier = ent.Comp.BlinkSkinColorMultiplier;
        cloneComp.BlinkEmoteId = [.. ent.Comp.BlinkEmoteId];
        cloneComp.StatePrefix = ent.Comp.StatePrefix;
        AddComp(args.CloneUid, cloneComp, true);
        _blindable.UpdateIsBlind(args.CloneUid);

        AddEyeAction((args.CloneUid, cloneComp), args.CloneUid);
    }

    [SubscribeLocalEvent]
    private void OnCloning(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<CloningEvent> args)
    {
        if (!args.Args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        _blindable.UpdateIsBlind(args.Args.CloneUid);
    }

    /// <summary>
    /// Handles pause accumulation for eye blinking.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<EyeBlinkingComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextOpenEyesTime += args.PausedTime;
        ent.Comp.NextBlinkingTime += args.PausedTime;
        foreach (var eyelid in ent.Comp.Eyelids)
        {
            eyelid.ScheduledCloseTime += args.PausedTime;
            eyelid.ScheduledOpenTime += args.PausedTime;
        }
    }
    #endregion Event Handlers

    #region Internal
    protected void SetEyelidsColor(Entity<EyeBlinkingComponent> eyeBlinking, Color? bodyColor)
    {
        var skinColor = bodyColor ?? Color.Pink;
        var blinkFade = eyeBlinking.Comp.BlinkSkinColorMultiplier;
        var eyelidColor = new Color(
            skinColor.R * blinkFade,
            skinColor.G * blinkFade,
            skinColor.B * blinkFade);

        eyeBlinking.Comp.EyelidsColor = eyelidColor;
        Dirty(eyeBlinking);
    }

    protected void SetStatusFlag(Entity<EyeBlinkingComponent> ent, BlinkStatus flag, bool set)
    {
        var prevStatus = ent.Comp.Status;

        if (set)
            ent.Comp.Status |= flag;
        else
            ent.Comp.Status &= ~flag;

        if (ent.Comp.Status == prevStatus)
            return;

        StatusChanged(ent, prevStatus);
        Dirty(ent);
    }

    /// <summary>
    /// Returns the entity that should be used for the eyes.
    /// For mobs that may have this component, this should be the entity proper.
    /// For organs, it should be their body.
    /// </summary>
    protected EntityUid GetActiveEntity(EntityUid eyes)
    {
        if (_organQuery.TryComp(eyes, out var organComp)
            && organComp.Body is { } body)
            return body;

        return eyes;
    }

    protected virtual void StatusChanged(Entity<EyeBlinkingComponent> ent, BlinkStatus oldValue) { }

    /// <summary>
    /// Adds the configured toggle action to the current holder.
    /// </summary>
    private void AddEyeAction(Entity<EyeBlinkingComponent> eyes, EntityUid holder)
    {
        if (eyes.Comp.EyeToggleActionEntity != null)
            return;

        _actions.AddAction(holder, ref eyes.Comp.EyeToggleActionEntity, eyes.Comp.EyeToggleAction);
        Dirty(eyes);
    }
    #endregion Internal
}

/// <summary>
/// Event raised when an entity blinks due to an emote (<see cref="EmoteEvent"/>).
/// </summary>
[Serializable, NetSerializable]
public sealed class BlinkEyeEvent(NetEntity netEntity) : EntityEventArgs
{
    /// <summary>
    /// The entity performing the blink.
    /// </summary>
    public readonly NetEntity NetEntity = netEntity;
}

/// <summary>
/// Event raised when an entity toggles their eyes open or closed via the ToggleEyesAction.
/// </summary>
public sealed partial class ToggleEyesActionEvent : InstantActionEvent;
