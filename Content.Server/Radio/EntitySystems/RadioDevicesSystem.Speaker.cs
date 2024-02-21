using Content.Server.Chat.Systems;
using Content.Server.Chat.V2;
using Content.Server.Radio.Components;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioDevicesSystem
{
    public  void InitializeSpeaker()
    {
        SubscribeLocalEvent<RadioSpeakerComponent, ActivateInWorldEvent>(OnActivateSpeaker);
        SubscribeLocalEvent<RadioSpeakerComponent, RadioEmittedEvent>(OnReceiveRadio);
    }

    private void OnActivateSpeaker(EntityUid uid, RadioSpeakerComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        SetSpeakerEnabled(uid, args.User, !component.Enabled, false, component);

        args.Handled = true;
    }

    private void OnReceiveRadio(EntityUid uid, RadioSpeakerComponent component, ref RadioEmittedEvent args)
    {
        if (!component.Enabled)
        {
            return;
        }

        if (args.Device == uid)
        {
            return;
        }

        var speaker = args.Speaker;

        var nameEv = new TransformSpeakerNameEvent(speaker, Name(speaker));
        RaiseLocalEvent(speaker, nameEv);

        _chat.SendBackgroundChatMessage(uid, args.Message, $"{Name(uid)} ({nameEv.Name})");
    }
}
