using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;

using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
/// This system handles radio speakers and microphones (which together form a hand-held radio).
/// </summary>
public abstract partial class SharedRadioDeviceSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedRadioSystem _radio = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    // Used to prevent a shitter from using a bunch of radios to spam chat.
    private readonly HashSet<(string, EntityUid, RadioChannelPrototype)> _recentlySent = [];

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    #region Component Init

    [SubscribeLocalEvent]
    private void OnMicrophoneInit(Entity<RadioMicrophoneComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Enabled)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnSpeakerInit(Entity<RadioSpeakerComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Enabled)
            EnsureComp<ActiveRadioComponent>(ent).Channels.UnionWith(ent.Comp.Channels);
        else
            RemCompDeferred<ActiveRadioComponent>(ent);
    }

    #endregion

    #region Toggling

    [SubscribeLocalEvent]
    private void OnActivateMicrophone(Entity<RadioMicrophoneComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!ent.Comp.ToggleOnInteract)
            return;

        ToggleRadioMicrophone(ent.AsNullable(), args.User, args.Handled);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnActivateSpeaker(Entity<RadioSpeakerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!ent.Comp.ToggleOnInteract)
            return;

        ToggleRadioSpeaker(ent.AsNullable(), args.User, args.Handled);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<RadioMicrophoneComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        SetMicrophoneEnabled(ent.AsNullable(), null, false, true);
    }

    /// <summary>
    /// Enables or disables a radio microphone.
    /// </summary>
    /// <param name="ent">The entity with the microphone.</param>
    /// <param name="user">The entity toggling the microphone, if any.</param>
    /// <param name="enabled">Whether the microphone should be enabled.</param>
    /// <param name="quiet">Whether to suppress the user-facing popup.</param>
    public void SetMicrophoneEnabled(
        Entity<RadioMicrophoneComponent?> ent,
        EntityUid? user,
        bool enabled,
        bool quiet = false
    )
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.PowerRequired && !_power.IsPowered(ent.Owner))
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        if (!quiet && user != null)
        {
            var state = Loc.GetString(ent.Comp.Enabled
                ? "handheld-radio-component-on-state"
                : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(ent, RadioDeviceVisuals.Broadcasting, ent.Comp.Enabled);
        if (ent.Comp.Enabled)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }

    #endregion

    #region Toggling

    /// <summary>
    /// Toggles a radio microphone.
    /// </summary>
    /// <param name="ent">The entity with the microphone.</param>
    /// <param name="user">The entity toggling the microphone.</param>
    /// <param name="quiet">Whether to suppress the user-facing popup.</param>
    public void ToggleRadioMicrophone(
        Entity<RadioMicrophoneComponent?> ent,
        EntityUid user,
        bool quiet = false
    )
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetMicrophoneEnabled(ent, user, !ent.Comp.Enabled, quiet);
    }

    /// <summary>
    /// Toggles a radio speaker.
    /// </summary>
    /// <param name="ent">The entity with the speaker.</param>
    /// <param name="user">The entity toggling the speaker.</param>
    /// <param name="quiet">Whether to suppress the user-facing popup.</param>
    public void ToggleRadioSpeaker(
        Entity<RadioSpeakerComponent?> ent,
        EntityUid user,
        bool quiet = false
    )
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetSpeakerEnabled(ent, user, !ent.Comp.Enabled, quiet);
    }

    /// <summary>
    /// Enables or disables a radio speaker.
    /// </summary>
    /// <param name="ent">The entity with the speaker.</param>
    /// <param name="user">The entity toggling the speaker, if any.</param>
    /// <param name="enabled">Whether the speaker should be enabled.</param>
    /// <param name="quiet">Whether to suppress the user-facing popup.</param>
    public void SetSpeakerEnabled(
        Entity<RadioSpeakerComponent?> ent,
        EntityUid? user,
        bool enabled,
        bool quiet = false
    )
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        if (!quiet && user != null)
        {
            var state = Loc.GetString(ent.Comp.Enabled
                ? "handheld-radio-component-on-state"
                : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(ent, RadioDeviceVisuals.Speaker, ent.Comp.Enabled);
        if (ent.Comp.Enabled)
            EnsureComp<ActiveRadioComponent>(ent).Channels.UnionWith(ent.Comp.Channels);
        else
            RemCompDeferred<ActiveRadioComponent>(ent);
    }

    #endregion

    [SubscribeLocalEvent]
    private void OnExamine(Entity<RadioMicrophoneComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = ProtoMan.Index(ent.Comp.BroadcastChannel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("radio-microphone-component-examine",
                ("color", proto.Color),
                ("channel", proto.LocalizedName),
                ("frequency", proto.Frequency)));
        }
    }

    [SubscribeLocalEvent]
    private void OnListen(Entity<RadioMicrophoneComponent> ent, ref ListenEvent args)
    {
        if (HasComp<RadioSpeakerComponent>(args.Source))
            return; // no feedback loops please.

        var channel = ProtoMan.Index(ent.Comp.BroadcastChannel);
        if (_recentlySent.Add((args.Message, args.Source, channel)))
            _radio.SendRadioMessage(args.Source, args.Message, channel, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnAttemptListen(Entity<RadioMicrophoneComponent> ent, ref ListenAttemptEvent args)
    {
        if (ent.Comp.PowerRequired && !_power.IsPowered(ent.Owner)
            || ent.Comp.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, ent.Owner, 0))
        {
            args.Cancel();
        }
    }

    [SubscribeLocalEvent]
    private void OnReceiveRadio(Entity<RadioSpeakerComponent> ent, ref RadioReceiveEvent args)
    {
        if (ent.Owner == args.RadioSource)
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(ent.Owner)),
            ("originalName", nameEv.VoiceName));

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chat.TrySendInGameICMessage(ent.Owner,
            args.Message,
            InGameICChatType.Whisper,
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: name,
            checkRadioPrefix: false);
    }

    [SubscribeLocalEvent]
    private void OnIntercomEncryptionChannelsChanged(
        Entity<IntercomComponent> ent,
        ref EncryptionChannelsChangedEvent args
    )
    {
        ent.Comp.SupportedChannels =
            args.Component.Channels.Select(p => new ProtoId<RadioChannelPrototype>(p)).ToList();

        var channel = args.Component.DefaultChannel;
        if (ent.Comp.CurrentChannel != null && ent.Comp.SupportedChannels.Contains(ent.Comp.CurrentChannel.Value))
            channel = ent.Comp.CurrentChannel;

        SetIntercomChannel(ent, channel);
    }

    [SubscribeLocalEvent]
    private void OnToggleIntercomMic(Entity<IntercomComponent> ent, ref ToggleIntercomMicMessage args)
    {
        if (ent.Comp.RequiresPower && !_power.IsPowered(ent.Owner))
            return;

        SetMicrophoneEnabled(ent.Owner, args.Actor, args.Enabled, true);
        ent.Comp.MicrophoneEnabled = args.Enabled;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleIntercomSpeaker(Entity<IntercomComponent> ent, ref ToggleIntercomSpeakerMessage args)
    {
        if (ent.Comp.RequiresPower && !_power.IsPowered(ent.Owner))
            return;

        SetSpeakerEnabled(ent.Owner, args.Actor, args.Enabled, true);
        ent.Comp.SpeakerEnabled = args.Enabled;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnSelectIntercomChannel(Entity<IntercomComponent> ent, ref SelectIntercomChannelMessage args)
    {
        if (ent.Comp.RequiresPower && !_power.IsPowered(ent.Owner))
            return;

        if (!ProtoMan.HasIndex<RadioChannelPrototype>(args.Channel) ||
            !ent.Comp.SupportedChannels.Contains(args.Channel))
            return;

        SetIntercomChannel(ent, args.Channel);
    }

    private void SetIntercomChannel(Entity<IntercomComponent> ent, ProtoId<RadioChannelPrototype>? channel)
    {
        ent.Comp.CurrentChannel = channel;

        if (TryComp<RadioMicrophoneComponent>(ent, out var mic))
        {
            if (channel == null)
            {
                SetMicrophoneEnabled(ent.Owner, null, false);
                ent.Comp.MicrophoneEnabled = false;
            }
            else
            {
                mic.BroadcastChannel = channel.Value;
                Dirty(ent, mic);
            }
        }

        if (TryComp<RadioSpeakerComponent>(ent, out var speaker))
        {
            if (channel == null)
            {
                SetSpeakerEnabled(ent.Owner, null, false);
                ent.Comp.SpeakerEnabled = false;
            }
            else
            {
                speaker.Channels = [channel.Value];
                Dirty(ent, speaker);
            }
        }

        Dirty(ent);
    }
}

