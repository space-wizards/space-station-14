using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.DeadSpace.Languages;
using Content.Shared.Corvax.TTS;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly AudioSystem _audio = default!; // DS14-TTS
    [Dependency] private readonly LanguageSystem _language = default!; // DS14-Languages
    [Dependency] private readonly IAdminManager _admin = default!; // DS14
    [Dependency] private readonly GameTicker _gameTicker = default!; // DS14

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<ActiveRadioComponent, RadioReceiveEvent>(OnActiveRadioReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            UpdateRadioChannels(uid, component);
        }
    }

    // DS14-TTS-Start
    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        var parent = Transform(uid).ParentUid;
        if (!parent.IsValid())
            return;

        var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
        RaiseLocalEvent(parent, ref relayEvent);

        HandleRadioReceive(
            receiver: parent,
            messageSource: args.MessageSource,
            chatMsg: args.ChatMsg,
            lexiconChatMsg: args.LexiconChatMsg,
            languageId: args.LanguageId,
            receiveSound: component.RadioReceiveSoundPath,
            true,
            args: args);
    }

    private void OnActiveRadioReceive(EntityUid uid, ActiveRadioComponent component, ref RadioReceiveEvent args)
    {
        HandleRadioReceive(
            receiver: uid,
            messageSource: args.MessageSource,
            chatMsg: args.ChatMsg,
            lexiconChatMsg: args.LexiconChatMsg,
            languageId: args.LanguageId,
            null,
            false,
            args: args);
    }

    private void HandleRadioReceive(
    EntityUid receiver,
    EntityUid messageSource,
    MsgChatMessage chatMsg, // DS14
    MsgChatMessage lexiconChatMsg,
    string? languageId,
    SoundSpecifier? receiveSound,
    bool sendMessage,
    RadioReceiveEvent args)
    {
        if (args.Receivers.Contains(receiver))
            return;

        // DS14-start
        if (TryComp(receiver, out ActorComponent? actor) &&
            !_gameTicker.UserHasJoinedGame(actor.PlayerSession))
        {
            return;
        }
        // DS14-end

        var msg = chatMsg;

        if (languageId != null && !_language.KnowsLanguage(receiver, languageId))
            msg = lexiconChatMsg;

        if (receiveSound != null)
            _audio.PlayPvs(receiveSound, receiver, AudioParams.Default.WithVolume(-10f));

        if (actor != null) // DS14
        {
            // DS14-start
            if (sendMessage)
            {
                if (ShouldSendCommandLinkSender(receiver, actor.PlayerSession, messageSource))
                    msg = WithCommandLinkSender(msg, messageSource);

                _netMan.ServerSendMessage(msg, actor.PlayerSession.Channel);
            }
            // DS14-end
            if (receiver != messageSource && TryComp(messageSource, out TTSComponent? _))
            {
                args.Receivers.Add(receiver);
            }
        }
    }

    // DS14-start
    private bool ShouldSendCommandLinkSender(EntityUid receiver, ICommonSession session, EntityUid source)
    {
        if (!HasComp<MobStateComponent>(source))
            return false;

        if (HasComp<StationAiHeldComponent>(receiver))
            return true;

        return _admin.IsAdmin(session) &&
               TryComp<GhostComponent>(receiver, out var ghost) &&
               ghost.CanGhostInteract;
    }

    private MsgChatMessage WithCommandLinkSender(MsgChatMessage message, EntityUid source)
    {
        var chat = message.Message;
        return new MsgChatMessage
        {
            Message = new ChatMessage(
                chat.Channel,
                chat.Message,
                chat.WrappedMessage,
                GetNetEntity(source),
                chat.SenderKey,
                chat.HideChat,
                chat.MessageColorOverride,
                chat.AudioPath,
                chat.AudioVolume),
        };
    }
    // DS14-end
    // DS14-TTS-End
}
