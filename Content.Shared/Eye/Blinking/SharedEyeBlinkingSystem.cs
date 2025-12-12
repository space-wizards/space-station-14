using Content.Shared.Chat;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

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
        var ev = new ChangeEyeStateEvent(GetNetEntity(ent.Owner), args.Blind);
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

[Serializable, NetSerializable]
public sealed class ChangeEyeStateEvent(NetEntity netEntity, bool eyesClosed) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
    public readonly bool EyesClosed = eyesClosed;
}

[Serializable, NetSerializable]
public sealed class BlinkEyeEvent(NetEntity netEntity) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
}
