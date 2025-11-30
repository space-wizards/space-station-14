using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Eye.Blinking;
public abstract partial class SharedEyeBlinkingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    override public void Initialize()
    {
        base.Initialize();
        EntityManager.ComponentAdded += OnComponentAdded;
        EntityManager.ComponentRemoved += OnComponentRemoved;
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentRemove>(OnBlinkingRemoved);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentShutdown>(OnBlinkingShutdown);
    }

    private void OnBlinkingRemoved(Entity<EyeBlinkingComponent> ent, ref ComponentRemove args)
    {
        OpenEyes(ent.Owner, ent.Comp);
    }

    private void OnBlinkingShutdown(Entity<EyeBlinkingComponent> ent, ref ComponentShutdown args)
    {
        OpenEyes(ent.Owner, ent.Comp);
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        var comp = args.BaseArgs.Component;
        var entUID = args.BaseArgs.Owner;
        if (!(comp is SleepingComponent)) return;
        if (TryComp<EyeBlinkingComponent>(entUID, out var eyeComp))
        {
            eyeComp.IsSleeping = false;

            OpenEyes(entUID, eyeComp);
            Dirty(entUID, eyeComp);
        }
    }

    private void OnComponentAdded(AddedComponentEventArgs args)
    {
        var comp = args.BaseArgs.Component;
        var entUID = args.BaseArgs.Owner;
        if (!(comp is SleepingComponent)) return;
        if (TryComp<EyeBlinkingComponent>(entUID, out var eyeComp))
        {
            eyeComp.IsSleeping = true;
            Blink(entUID, eyeComp, _timing.CurTime);
            Dirty(entUID, eyeComp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EyeBlinkingComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;
            if (comp.IsSleeping)
                continue;
            if (!comp.IsBlinking)
            {
                if (comp.NextBlinkingTime <= curTime)
                    Blink(uid, comp, curTime);
            }
            else
            {
                if (comp.NextOpenEyesTime <= curTime)
                    OpenEyes(uid, comp);
            }
        }
    }

    private void Blink(EntityUid uid, EyeBlinkingComponent comp, TimeSpan curTime)
    {
        comp.IsBlinking = true;
        comp.NextOpenEyesTime = curTime + comp.BlinkDuration;
        comp.NextBlinkingTime = curTime + comp.BlinkInterval + comp.BlinkDuration;
        Dirty(uid, comp);
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;
        UpdateAppearance(uid, appearance, true);
    }

    private void OpenEyes(EntityUid uid, EyeBlinkingComponent comp)
    {
        comp.IsBlinking = false;
        Dirty(uid, comp);
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;
        UpdateAppearance(uid, appearance, false);
    }

    protected virtual void UpdateAppearance(EntityUid uid, AppearanceComponent appearance, bool isBlinking)
    {
        _appearance.SetData(uid, SichEyeBlinkingVisuals.Blinking, isBlinking, appearance);
    }
}

[Serializable, NetSerializable]
public enum SichEyeBlinkingVisuals : byte
{
    Blinking
}
