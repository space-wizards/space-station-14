using Content.Shared.Chat;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;

namespace Content.Shared.Eye.Blinking;
public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

        ent.Comp.NextBlinkingTime = _timing.CurTime;
        Dirty(ent);
    }

    private void MobStateChangedEventHandler(Entity<EyeBlinkingComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent, args.NewMobState != MobState.Dead);
    }

    private void BlindnessChangedEventHanlder(Entity<EyeBlinkingComponent> ent, ref BlindnessChangedEvent args)
    {
        _appearance.SetData(ent, EyeBlinkingVisuals.EyesClosed, args.Blind);
    }

    private void SetEnabled(Entity<EyeBlinkingComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        if (enabled)
            ResetBlink(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (comp.NextBlinkingTime > curTime)
                continue;
            Blink((uid, comp));
        }
    }

    public virtual void Blink(Entity<EyeBlinkingComponent> ent)
    {
        ResetBlink(ent);
    }

    protected virtual void ResetBlink(Entity<EyeBlinkingComponent> ent)
    {
        ent.Comp.NextBlinkingTime = _timing.CurTime + ent.Comp.BlinkInterval + ent.Comp.BlinkDuration;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public enum EyeBlinkingVisuals : byte
{
    EyesClosed
}
