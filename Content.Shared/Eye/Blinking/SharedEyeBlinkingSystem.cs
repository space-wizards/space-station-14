using Content.Shared.Chat;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.Eye.Blinking;

public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, BlindnessChangedEvent>(BlindnessChangedEventHanlder);
        SubscribeLocalEvent<EyeBlinkingComponent, MobStateChangedEvent>(MobStateChangedEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, EmoteEvent>(EmoteEventHandler);
    }

    public void EmoteEventHandler(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.ID != ent.Comp.BlinkEmoteId)
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
        ent.Comp.EyesClosed = args.Blind;
        var ev = new EyeStateChangedEvent(GetNetEntity(ent.Owner), args.Blind);
        RaiseNetworkEvent(ev);
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
/// Event raised when the <see cref="BlindnessChangedEvent"/> is triggered on an entity.
/// Used to synchronize the visual state of the eyes (open/closed) based on blindness.
/// </summary>
[Serializable, NetSerializable]
public sealed class EyeStateChangedEvent(NetEntity netEntity, bool eyesClosed) : EntityEventArgs
{
    /// <summary>
    /// The entity whose blindness/sight state has changed.
    /// </summary>
    public readonly NetEntity NetEntity = netEntity;

    /// <summary>
    /// Indicates whether the eyes are currently closed.
    /// </summary>
    public readonly bool EyesClosed = eyesClosed;
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
