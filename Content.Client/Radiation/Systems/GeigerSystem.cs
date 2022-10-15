using Content.Client.Items;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);

        SubscribeLocalEvent<OnGeigerSoundDoneEvent>(OnGeigerSoundDone);
    }

    private void OnGeigerSoundDone(OnGeigerSoundDoneEvent ev)
    {
        //var param = AudioParams.Default.WithDoneEvent(ev);
        //_audio.Play("/Audio/Items/Geiger/ext1.ogg", Filter.Local(), ev.GeigerUid, param);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        var curLevel = RadsToLevel(component.CurrentRadiation);
        var newLevel = RadsToLevel(state.CurrentRadiation);

        if (curLevel != newLevel)
        {
            if (component.Stream == null)
            {
                var ev = new OnGeigerSoundDoneEvent(uid);
                var param = AudioParams.Default.WithDoneEvent(ev).WithLoop(true);
                component.Stream = _audio.Play("/Audio/Items/Geiger/ext1.ogg", Filter.Local(), uid, param);
            }
            else
            {
                //component.Stream.Stop();
            }
        }

        component.CurrentRadiation = state.CurrentRadiation;
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
