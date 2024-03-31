using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Audio;

public abstract class SharedAmbientSoundSystem : EntitySystem
{
    private EntityQuery<AmbientSoundComponent> _query;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientSoundComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<AmbientSoundComponent, ComponentHandleState>(HandleCompState);
        _query = GetEntityQuery<AmbientSoundComponent>();
    }

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

    private void HandleCompState(EntityUid uid, AmbientSoundComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AmbientSoundComponentState state) return;
        SetAmbience(uid, state.Enabled, component);
        SetRange(uid, state.Range, component);
        SetVolume(uid, state.Volume, component);
        SetSound(uid, state.Sound, component);
    }

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
}
