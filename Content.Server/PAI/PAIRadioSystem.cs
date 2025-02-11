using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Radio;
using Content.Shared.PAI;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.PAI;

public sealed class PAIRadioSystem : SharedPAIRadioSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PAIRadioComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<PAIRadioComponent, EntitySpokeEvent>(OnSpeak);
        SubscribeLocalEvent<PAIRadioComponent, RadioReceiveEvent>(OnReceive);
    }

    private void OnKeysChanged(EntityUid uid, PAIRadioComponent comp, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, comp, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, PAIRadioComponent comp, EncryptionKeyHolderComponent? keyHolder = null)
    {
        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = keyHolder.Channels;
    }

    private void OnSpeak(EntityUid uid, PAIRadioComponent comp, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(uid, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnReceive(EntityUid uid, PAIRadioComponent comp, ref RadioReceiveEvent args)
    {
        if (TryComp<ActorComponent>(uid, out var actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
    }
}
