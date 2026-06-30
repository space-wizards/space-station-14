using Content.Shared.Body;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Content.Shared.Cloning.Events;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Eye.Blinking;

public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _apperance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, BlindnessChangedEvent>(BlindnessChangedEventHanlder);
        SubscribeLocalEvent<EyeBlinkingComponent, MobStateChangedEvent>(MobStateChangedEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, AfterChangelingTransformEvent>(AfterChangelingTransformEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, EmoteEvent>(EmoteEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, ApplyOrganMarkingsEvent>(OnApplyOrganMarkingEvent);

    }

    private void OnApplyOrganMarkingEvent(Entity<EyeBlinkingComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        SetEyelidsColor(ent);
    }

    private void SetEyelidsColor(Entity<EyeBlinkingComponent> ent)
    {
        var eyelidColor = Color.Red;
        if (!TryComp<BodyComponent>(ent.Owner, out var body)) return;

        VisualOrganComponent? visualHead = null;
        foreach (var organ in body.Organs?.ContainedEntities ?? Array.Empty<EntityUid>())
        {
            if (!TryComp<OrganComponent>(organ, out var organComp))
                continue;
            if (organComp.Category != "Head")
            {
                continue;
            }
            visualHead = CompOrNull<VisualOrganComponent>(organ);
            if (visualHead != null)
                break;
        }

        var skinColor = visualHead?.Profile.SkinColor ?? Color.Pink;
        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        eyelidColor = new Color(
            skinColor.R * blinkFade,
            skinColor.G * blinkFade,
            skinColor.B * blinkFade);

        ent.Comp.EyelidsColor = eyelidColor;
        Dirty(ent);
    }

    private void AfterChangelingTransformEventHandler(Entity<EyeBlinkingComponent> ent, ref AfterChangelingTransformEvent args)
    {
        ent.Comp.Enabled = true;
        Dirty(ent);
    }

    public void EmoteEventHandler(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (!ent.Comp.BlinkEmoteId.Contains(args.Emote.ID))
            return;

        if (!ent.Comp.Enabled)
            return;

        var ev = new BlinkEyeEvent(GetNetEntity(ent.Owner));
        RaiseNetworkEvent(ev);
    }

    private void MobStateChangedEventHandler(Entity<EyeBlinkingComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent, args.NewMobState != MobState.Dead);
    }

    public virtual void BlindnessChangedEventHanlder(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        _apperance.SetData(ent, EyeBlinkingVisuals.EyesClosed, args.Blind);
        SetEnabled(ent, !args.Blind);
    }

    private void SetEnabled(Entity<EyeBlinkingComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }
}

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

[Serializable, NetSerializable]
public sealed class UpdateEyelidsAfterApplyOrganMarkingsEvent(NetEntity entity) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
}
