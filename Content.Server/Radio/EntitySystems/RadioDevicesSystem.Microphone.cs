using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Radio;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioDevicesSystem
{
    // Used to prevent shitters from using a bunch of radios to spam chat.
    private HashSet<(string, EntityUid)> _recentlySent = new();

    public void InitializeMicrophone()
    {
        SubscribeLocalEvent<RadioMicrophoneComponent, ComponentInit>(OnMicrophoneInit);
        SubscribeLocalEvent<RadioMicrophoneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioMicrophoneComponent, ActivateInWorldEvent>(OnActivateMicrophone);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    private void OnMicrophoneInit(EntityUid uid, RadioMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    private void OnActivateMicrophone(EntityUid uid, RadioMicrophoneComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        SetMicrophoneEnabled(uid, args.User, !component.Enabled, false, component);

        args.Handled = true;
    }

    private void OnPowerChanged(EntityUid uid, RadioMicrophoneComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        SetMicrophoneEnabled(uid, null, false,  true, component);
    }

    private void OnExamine(EntityUid uid, RadioMicrophoneComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine", ("frequency", proto.Frequency)));
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void OnListen(EntityUid uid, RadioMicrophoneComponent component, ListenEvent args)
    {
        if (!component.Enabled)
        {
            return;
        }

        if (HasComp<RadioSpeakerComponent>(args.Source))
            return; // no feedback loops please.

        if (_recentlySent.Add((args.Message, args.Source)))
            _chat.SendRadioMessageViaDevice(args.Source, args.Message, _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel), uid);
    }

    private void OnAttemptListen(EntityUid uid, RadioMicrophoneComponent component, ListenAttemptEvent args)
    {
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager)
            || component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0))
            args.Cancel();
    }
}
