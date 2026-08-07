using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Cloning.Events;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Eye.Blinking;

public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private BlindableSystem _blindableSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<EyeBlinkingComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.EyeToggleActionEntity == null)
            return;

        if (TryComp<OrganComponent>(ent, out var organComp))
        {
            if (organComp.Body is { } body)
                _actionsSystem.RemoveAction(body, ent.Comp.EyeToggleActionEntity);
        }
        else
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.EyeToggleActionEntity);
        }
    }

    [SubscribeLocalEvent]
    private void OnSleepStateChanged(Entity<EyeBlinkingComponent> ent, ref SleepStateChangedEvent args)
    {
        ent.Comp.EyesClosed = args.FellAsleep;
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
        SetEyelidsColor(ent, args.Base);

        if (ent.Comp.EyeToggleActionEntity == null)
            _actionsSystem.AddAction(ent.Owner, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
    }

    [SubscribeLocalEvent]
    private void OnApplyOrganProfileData(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<ApplyOrganProfileDataEvent> args)
    {
        SetEyelidsColor(ent, args.Args.Base);

        if (ent.Comp.EyeToggleActionEntity == null)
            _actionsSystem.AddAction(args.Body.Owner, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
    }

    [SubscribeLocalEvent]
    private void OnOrganCopyAppearance(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<OrganCopyAppearanceEvent> args)
    {
        if (!TryComp<VisualOrganComponent>(args.Args.Organ, out var visualOrgan))
            return;
        SetEyelidsColor(ent, visualOrgan.Profile);

        if (ent.Comp.EyeToggleActionEntity == null)
            _actionsSystem.AddAction(args.Body.Owner, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
    }

    private void SetEyelidsColor(Entity<EyeBlinkingComponent> eyeBlinking, OrganProfileData? organProfile)
    {
        var skinColor = organProfile?.SkinColor ?? Color.Pink;
        var blinkFade = eyeBlinking.Comp.BlinkSkinColorMultiplier;
        var eyelidColor = new Color(
            skinColor.R * blinkFade,
            skinColor.G * blinkFade,
            skinColor.B * blinkFade);

        eyeBlinking.Comp.EyelidsColor = eyelidColor;
        Dirty(eyeBlinking);
        var ev = new InitEyesEvent(GetNetEntity(eyeBlinking.Owner), eyelidColor);
        RaiseNetworkEvent(ev);
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        OnEmote(ent, args.Emote.ID);
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<EmoteEvent> args)
    {
        var emoteArgs = args.Args;

        if (emoteArgs.Handled)
            return;

        emoteArgs.Handled = true;
        args.Args = emoteArgs;

        OnEmote(ent, args.Args.Emote.ID);
    }

    private void OnEmote(Entity<EyeBlinkingComponent> ent, string emoteId)
    {
        if (!ent.Comp.BlinkEmoteId.Contains(emoteId))
            return;

        if (!ent.Comp.Enabled)
            return;

        var ev = new BlinkEyeEvent(GetNetEntity(ent.Owner));
        RaiseNetworkEvent(ev);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<EyeBlinkingComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent, args.NewMobState != MobState.Dead);

        // Remove action if entity dead.
        if (args.NewMobState == MobState.Dead)
        {
            if (ent.Comp.EyeToggleActionEntity != null)
            {
                _actionsSystem.RemoveAction(args.Target, ent.Comp.EyeToggleActionEntity);
            }
            return;
        }
        if (ent.Comp.EyeToggleActionEntity == null)
        {
            _actionsSystem.AddAction(args.Target, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
        }
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
            _actionsSystem.SetToggled(ent.Comp.EyeToggleActionEntity, args.Blind);

        if (!args.Blind)
        {
            _appearance.RemoveData(ent.Owner, EyeBlinkingVisuals.EyesClosed);
            var ev = new OpenEyesEvent(GetNetEntity(ent.Owner));
            RaiseNetworkEvent(ev);
        }
        else
        {
            _appearance.SetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, args.Blind);
        }

        SetEnabled(ent, !args.Blind);
    }

    [SubscribeLocalEvent]
    private void OnBlindnessChanged(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<BlindnessChangedEvent> args)
    {
        var blindnessArgs = args.Args;
        OnBlindnessChanged(ent, ref blindnessArgs);
        args.Args = blindnessArgs;
    }

    private void SetEnabled(Entity<EyeBlinkingComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<EyeBlinkingComponent> ent, ref ToggleEyesActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.EyesClosed = !ent.Comp.EyesClosed;

        _blindableSystem.UpdateIsBlind(args.Performer);
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
        if (ent.Comp.EyesClosed)
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

        cloneComp.Enabled = true;
        cloneComp.EyesClosed = false;

        cloneComp.EyelidsSprite = ent.Comp.EyelidsSprite;
        cloneComp.EyelidsColor = ent.Comp.EyelidsColor;

        cloneComp.MaxAsyncBlink = ent.Comp.MaxAsyncBlink;
        cloneComp.MaxAsyncOpenBlink = ent.Comp.MaxAsyncOpenBlink;

        cloneComp.MinBlinkDuration = ent.Comp.MinBlinkDuration;
        cloneComp.MinBlinkInterval = ent.Comp.MinBlinkInterval;

        cloneComp.BlinkSkinColorMultiplier = ent.Comp.BlinkSkinColorMultiplier;
        AddComp(args.CloneUid, cloneComp, true);
        _blindableSystem.UpdateIsBlind(args.CloneUid);

        var ev = new InitEyesEvent(GetNetEntity(ent.Owner), ent.Comp.EyelidsColor ?? Color.White);
        RaiseNetworkEvent(ev);
    }

    [SubscribeLocalEvent]
    private void OnCloning(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<CloningEvent> args)
    {
        if (!args.Args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;
        var eyes = ent.Owner;
        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<EyeBlinkingComponent>();
        cloneComp.EyeToggleAction = ent.Comp.EyeToggleAction;

        cloneComp.Enabled = true;
        cloneComp.EyesClosed = false;

        cloneComp.EyelidsSprite = ent.Comp.EyelidsSprite;
        cloneComp.EyelidsColor = ent.Comp.EyelidsColor;

        cloneComp.MaxAsyncBlink = ent.Comp.MaxAsyncBlink;
        cloneComp.MaxAsyncOpenBlink = ent.Comp.MaxAsyncOpenBlink;

        cloneComp.MinBlinkDuration = ent.Comp.MinBlinkDuration;
        cloneComp.MinBlinkInterval = ent.Comp.MinBlinkInterval;

        cloneComp.BlinkSkinColorMultiplier = ent.Comp.BlinkSkinColorMultiplier;
        AddComp(eyes, cloneComp, true);
        _blindableSystem.UpdateIsBlind(args.Args.CloneUid);

        var ev = new InitEyesEvent(GetNetEntity(ent.Owner), ent.Comp.EyelidsColor ?? Color.White);
        RaiseNetworkEvent(ev);
    }
}

/// <summary>
/// Enum for force closing the eyes of an entity by Appearance system.
/// </summary>
[Serializable, NetSerializable]
public enum EyeBlinkingVisuals : byte
{
    EyesClosed,
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
/// Event raised when an entity toggles their eyes open or closed via the <see cref="ToggleEyesAction"/>.
/// </summary>
public sealed partial class ToggleEyesActionEvent : InstantActionEvent;

/// <summary>
/// Open Eyes after remove EyeClosing from appearanceData
/// </summary>
[Serializable, NetSerializable]
public sealed partial class OpenEyesEvent(NetEntity netEntity) : EntityEventArgs
{
    /// <summary>
    /// The entity performing the open Eye.
    /// </summary>
    public readonly NetEntity NetEntity = netEntity;
}

/// <summary>
/// Open Eyes after remove EyeClosing from appearanceData
/// </summary>
[Serializable, NetSerializable]
public sealed partial class InitEyesEvent(NetEntity netEntity, Color eyelidsColor) : EntityEventArgs
{
    /// <summary>
    /// The entity performing init eyes.
    /// </summary>
    public readonly NetEntity NetEntity = netEntity;
    /// <summary>
    /// Eyelids color of the entity performing the init eyes.
    /// </summary>
    public readonly Color EyelidsColor = eyelidsColor;
}
