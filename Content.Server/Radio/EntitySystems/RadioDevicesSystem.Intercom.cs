using Content.Server.Power.EntitySystems;
using Content.Server.Radio.Components;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.UserInterface;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioDevicesSystem
{
    public void InitializeIntercom()
    {
        SubscribeLocalEvent<IntercomComponent, BeforeActivatableUIOpenEvent>(OnBeforeIntercomUiOpen);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomMicMessage>(OnToggleIntercomMic);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomSpeakerMessage>(OnToggleIntercomSpeaker);
        SubscribeLocalEvent<IntercomComponent, SelectIntercomChannelMessage>(OnSelectIntercomChannel);
    }

     private void OnBeforeIntercomUiOpen(EntityUid uid, IntercomComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateIntercomUi(uid, component);
    }

    private void OnToggleIntercomMic(EntityUid uid, IntercomComponent component, ToggleIntercomMicMessage args)
    {
        if (component.RequiresPower && !this.IsPowered(uid, EntityManager) || args.Session.AttachedEntity is not { } user)
            return;

        SetMicrophoneEnabled(uid, user, args.Enabled, true);
        UpdateIntercomUi(uid, component);
    }

    private void OnToggleIntercomSpeaker(EntityUid uid, IntercomComponent component, ToggleIntercomSpeakerMessage args)
    {
        if (component.RequiresPower && !this.IsPowered(uid, EntityManager) || args.Session.AttachedEntity is not { } user)
            return;

        SetSpeakerEnabled(uid, user, args.Enabled, true);
        UpdateIntercomUi(uid, component);
    }

    private void OnSelectIntercomChannel(EntityUid uid, IntercomComponent component, SelectIntercomChannelMessage args)
    {
        if (component.RequiresPower && !this.IsPowered(uid, EntityManager) || args.Session.AttachedEntity is not { })
            return;

        if (!_protoMan.TryIndex<RadioChannelPrototype>(args.Channel, out _) || !component.SupportedChannels.Contains(args.Channel))
            return;

        if (TryComp<RadioMicrophoneComponent>(uid, out var mic))
            mic.BroadcastChannel = args.Channel;
        if (TryComp<RadioSpeakerComponent>(uid, out var speaker))
            speaker.Channels = new(){ args.Channel };

        var comp = EnsureComp<InternalRadioComponent>(uid);
        comp.ReceiveChannels = new HashSet<string>() {args.Channel};
        comp.SendChannels = new HashSet<string>() {args.Channel};

        UpdateIntercomUi(uid, component);
    }

    private void UpdateIntercomUi(EntityUid uid, IntercomComponent component)
    {
        var micComp = CompOrNull<RadioMicrophoneComponent>(uid);
        var speakerComp = CompOrNull<RadioSpeakerComponent>(uid);

        var micEnabled = micComp?.Enabled ?? false;
        var speakerEnabled = speakerComp?.Enabled ?? false;
        var availableChannels = component.SupportedChannels;
        var selectedChannel = micComp?.BroadcastChannel ?? SharedChatSystem.CommonChannel;
        var state = new IntercomBoundUIState(micEnabled, speakerEnabled, availableChannels, selectedChannel);
        _ui.TrySetUiState(uid, IntercomUiKey.Key, state);
    }
}
