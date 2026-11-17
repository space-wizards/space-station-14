using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Audio;

public abstract partial class SharedAmbientSoundSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<AmbientSoundComponent> _query = default!;

    public virtual void SetAmbience(EntityUid uid, bool value, AmbientSoundComponent? ambience = null)
    {
        if (!_query.Resolve(uid, ref ambience, false) || ambience.Enabled == value)
            return;

        ambience.Enabled = value;
        QueueUpdate(uid, ambience);
        Dirty(uid, ambience);
    }

    public virtual void SetRange(EntityUid uid, float value, AmbientSoundComponent? ambience = null)
    {
        if (!_query.Resolve(uid, ref ambience, false) || MathHelper.CloseToPercent(ambience.Range, value))
            return;

        ambience.Range = value;
        QueueUpdate(uid, ambience);
        Dirty(uid, ambience);
    }

    protected virtual void QueueUpdate(EntityUid uid, AmbientSoundComponent ambience)
    {
        // client side tree
    }

    public virtual void SetVolume(EntityUid uid, float value, AmbientSoundComponent? ambience = null)
    {
        if (!_query.Resolve(uid, ref ambience, false) || MathHelper.CloseToPercent(ambience.Volume, value))
            return;

        ambience.Volume = value;
        Dirty(uid, ambience);
    }

    public virtual void SetSound(EntityUid uid, SoundSpecifier sound, AmbientSoundComponent? ambience = null)
    {
        if (!_query.Resolve(uid, ref ambience, false) || ambience.Sound == sound)
            return;

        ambience.Sound = sound;
        QueueUpdate(uid, ambience);
        Dirty(uid, ambience);
    }

    [SubscribeLocalEvent]
    private void HandleCompState(EntityUid uid, AmbientSoundComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AmbientSoundComponentState state) return;
        SetAmbience(uid, state.Enabled, component);
        SetRange(uid, state.Range, component);
        SetVolume(uid, state.Volume, component);
        SetSound(uid, state.Sound, component);
    }

    [SubscribeLocalEvent]
    private void GetCompState(EntityUid uid, AmbientSoundComponent component, ref ComponentGetState args)
    {
        args.State = new AmbientSoundComponentState
        {
            Enabled = component.Enabled,
            Range = component.Range,
            Volume = component.Volume,
            Sound = component.Sound,
        };
    }

    [SubscribeLocalEvent]
    private void OnGrowingInit(Entity<GrowingAmbientSoundComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextGrowth = _timing.CurTime + ent.Comp.GrowthDelay;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GrowingAmbientSoundComponent, AmbientSoundComponent>();

        while (query.MoveNext(out var uid, out var growingComp, out var ambientSound))
        {
            if (growingComp.NextGrowth < _timing.CurTime)
                continue;

            growingComp.NextGrowth += growingComp.GrowthDelay;
            Dirty(uid, growingComp);

            if (growingComp.RangeGrowth != null)
                SetRange(uid, Math.Clamp(ambientSound.Range + growingComp.RangeGrowth.Value, growingComp.MinRange, growingComp.MaxRange), ambientSound);

            if (growingComp.VolumeGrowth != null)
                SetVolume(uid, Math.Clamp(ambientSound.Volume + growingComp.VolumeGrowth.Value, growingComp.MinVolume, growingComp.MaxVolume), ambientSound);
        }
    }
}
