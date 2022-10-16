using Content.Client.Items;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);

        SubscribeLocalEvent<OnGeigerSoundDoneEvent>(OnGeigerSoundDone);
    }

    private void OnGeigerSoundDone(OnGeigerSoundDoneEvent ev)
    {
        PlayGeigerSound(ev.GeigerUid);
    }

    private void PlayGeigerSound(EntityUid uid, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Sounds.TryGetValue(component.DangerLevel, out var sounds))
        {
            component.Stream = null;
            return;
        }

        var sound = sounds.GetSound(_random, _proto);
        var ev = new OnGeigerSoundDoneEvent(uid);
        var param = sounds.Params.WithDoneEvent(ev);

        component.Stream = _audio.Play(sound, Filter.Local(), uid, param);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        var curLevel = component.DangerLevel;
        var newLevel = state.DangerLevel;
        component.DangerLevel = state.DangerLevel;

        if (curLevel != newLevel)
        {
            component.Stream?.Stop();
            if (component.Stream == null)
            {
                PlayGeigerSound(uid, component);
            }
        }

        component.CurrentRadiation = state.CurrentRadiation;
        component.DangerLevel = state.DangerLevel;
        component.UiUpdateNeeded = true;


    }

    private void OnGetStatusMessage(EntityUid uid, GeigerComponent component, ItemStatusCollectMessage args)
    {
        if (!component.ShowControl)
            return;

        args.Controls.Add(new GeigerItemControl(component));
    }
}

public sealed class OnGeigerSoundDoneEvent : EntityEventArgs
{
    public readonly EntityUid GeigerUid;

    public OnGeigerSoundDoneEvent(EntityUid geigerUid)
    {
        GeigerUid = geigerUid;
    }
}
