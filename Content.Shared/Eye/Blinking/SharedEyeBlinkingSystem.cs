using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Graphics;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Eye.Blinking;

public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _apperance = default!;
    [Dependency] private BlindableSystem _blindableSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, BlindnessChangedEvent>(OnBlindnessChanged);
        SubscribeLocalEvent<EyeBlinkingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EyeBlinkingComponent, AfterChangelingTransformEvent>(OnAfterChangelingTransform);
        SubscribeLocalEvent<EyeBlinkingComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<EyeBlinkingComponent, ApplyOrganMarkingsEvent>(OnApplyOrganMarking);

        SubscribeLocalEvent<EyeBlinkingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EyeBlinkingComponent, ToggleEyesActionEvent>(OnToggleAction);
        SubscribeLocalEvent<EyeBlinkingComponent, CanSeeAttemptEvent>(OnTrySee);

        SubscribeLocalEvent<EyeBlinkingComponent, SleepStateChangedEvent>(OnSleepStateChanged);

    }

    private void OnSleepStateChanged(Entity<EyeBlinkingComponent> ent, ref SleepStateChangedEvent args)
    {
        ent.Comp.EyesClosed = args.FellAsleep;
    }

    private void OnApplyOrganMarking(Entity<EyeBlinkingComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        SetEyelidsColor(ent);
    }

    private void SetEyelidsColor(Entity<EyeBlinkingComponent> ent)
    {
        var eyelidColor = Color.Red;

        if (!TryComp<BodyComponent>(ent.Owner, out var body)) return;

        VisualOrganComponent? visualHead = null;

        // Obtains the "head" organ component in order to retrieve the character's skin color from it.
        foreach (var organ in body.Organs?.ContainedEntities ?? Array.Empty<EntityUid>())
        {
            if (!TryComp<OrganComponent>(organ, out var organComp))
                continue;

            if (organComp.Category != "Head")
                continue;

            visualHead = CompOrNull<VisualOrganComponent>(organ);
            if (visualHead != null)
                break;
        }
        // Gets the skin color from VisualOrganComponent, or returns pink as a fallback color if not found.
        var skinColor = visualHead?.Profile.SkinColor ?? Color.Pink;
        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        eyelidColor = new Color(
            skinColor.R * blinkFade,
            skinColor.G * blinkFade,
            skinColor.B * blinkFade);

        ent.Comp.EyelidsColor = eyelidColor;
        Dirty(ent);
    }

    /// <summary>
    /// Handles changeling transformation/cloning and enables the component if it was copied from a dead original in a disabled state.
    /// </summary>
    private void OnAfterChangelingTransform(Entity<EyeBlinkingComponent> ent, ref AfterChangelingTransformEvent args)
    {
        ent.Comp.Enabled = true;
        Dirty(ent);
    }

    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (!ent.Comp.BlinkEmoteId.Contains(args.Emote.ID))
            return;

        if (!ent.Comp.Enabled)
            return;

        var ev = new BlinkEyeEvent(GetNetEntity(ent.Owner));
        RaiseNetworkEvent(ev);
    }

    private void OnMobStateChanged(Entity<EyeBlinkingComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent, args.NewMobState != MobState.Dead);
    }

    private void OnBlindnessChanged(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        ent.Comp.EyesClosed = args.Blind;

        if (ent.Comp.EyeToggleActionEntity != null)
            _actionsSystem.SetToggled(ent.Comp.EyeToggleActionEntity, ent.Comp.EyesClosed);

        if (!args.Blind)
        {
            _apperance.RemoveData(ent, EyeBlinkingVisuals.EyesClosed);
            var ev = new OpenEyesEvent(GetNetEntity(ent.Owner));
            RaiseNetworkEvent(ev);
        }
        else
        {
            _apperance.SetData(ent, EyeBlinkingVisuals.EyesClosed, args.Blind);
        }

        SetEnabled(ent, !args.Blind);
    }

    private void SetEnabled(Entity<EyeBlinkingComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }

    private void OnMapInit(Entity<EyeBlinkingComponent> eye, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(eye, ref eye.Comp.EyeToggleActionEntity, eye.Comp.EyeToggleAction);
        Dirty(eye);
    }

    private void OnShutdown(Entity<EyeBlinkingComponent> eye, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(eye.Owner, eye.Comp.EyeToggleActionEntity);
    }

    private void OnToggleAction(Entity<EyeBlinkingComponent> eye, ref ToggleEyesActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        eye.Comp.EyesClosed = !eye.Comp.EyesClosed;

        _blindableSystem.UpdateIsBlind(eye.Owner);
    }

    private void OnTrySee(Entity<EyeBlinkingComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.EyesClosed)
            args.Cancel();
    }

}

/// <summary>
/// Enum for force closing the eyes of an entity by Apperance system.
/// </summary>
[Serializable, NetSerializable]
public enum EyeBlinkingVisuals : byte
{
    EyesClosed
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

public sealed partial class ToggleEyesActionEvent : InstantActionEvent;

/// <summary>
/// Open Eyes after remove EyeClosing from apperanceData
/// </summary>
[Serializable, NetSerializable]
public sealed partial class OpenEyesEvent(NetEntity netEntity) : EntityEventArgs
{
    /// <summary>
    /// The entity performing the open Eye.
    /// </summary>
    public readonly NetEntity NetEntity = netEntity;
}
