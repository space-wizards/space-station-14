using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const char RadioCommonPrefix = ';';
    public const char RadioChannelPrefix = ':';
    public const char RadioChannelAltPrefix = '.';
    public const char LocalPrefix = '>';
    public const char ConsolePrefix = '/';
    public const char DeadPrefix = '\\';
    public const char LoocPrefix = '(';
    public const char OOCPrefix = '[';
    public const char EmotesPrefix = '@';
    public const char EmotesAltPrefix = '*';
    public const char AdminPrefix = ']';
    public const char WhisperPrefix = ',';
    public const char DefaultChannelKey = 'h';

    [ValidatePrototypeId<RadioChannelPrototype>]
    public const string CommonChannel = "Common";

    public static string DefaultChannelPrefix = $"{RadioChannelPrefix}{DefaultChannelKey}";

    /// <summary>
    /// Cache of the keycodes for faster lookup.
    /// </summary>
    private FrozenDictionary<char, RadioChannelPrototype> _keyCodes = default!;

    private static string SanitizeMessageCapital(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Capitalize first letter
        message = char.ToUpper(message[0]) + message.Remove(0, 1);
        return message;
    }

    public static string InjectTagInsideTag(string message, string outerTag, string innerTag, string? tagParameter)
    {
        var tagStart = message.IndexOf($"[{outerTag}]");
        var tagEnd = message.IndexOf($"[/{outerTag}]");
        if (tagStart < 0 || tagEnd < 0) //If the outer tag is not found, the injection is not performed
            return message;
        tagStart += outerTag.Length + 2;

        string innerTagProcessed = tagParameter != null ? $"[{innerTag}={tagParameter}]" : $"[{innerTag}]";

        message = message.Insert(tagEnd, $"[/{innerTag}]");
        message = message.Insert(tagStart, innerTagProcessed);

        return message;
    }

    public static string FilterAccidentalInput(string input)
    {
        var trim = input.ToLower().Trim();

        // TODO: 't' is the shortcut used to open chat; find this magic string's owner!
        if (trim[0] != 't' || trim.Length < 2)
            return input;

        switch (trim[1])
        {
            case ConsolePrefix:
                return input[2..];
            case LoocPrefix:
                return input[2..];
            case OOCPrefix:
                return input[2..];
            case AdminPrefix:
                return input[2..];
            case EmotesPrefix:
                return input[2..];
            case DeadPrefix:
                return input[2..];
            case LocalPrefix:
                return input[2..];
            case RadioCommonPrefix:
            case RadioChannelPrefix:
                return input[1..];
            case WhisperPrefix:
                return input[2..];
            default:
                return input;
        }
    }

    public void InitializeRadio()
    {
        DebugTools.Assert(_prototypeManager.HasIndex<RadioChannelPrototype>(CommonChannel));
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        CacheRadios();
    }

    protected virtual void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<RadioChannelPrototype>())
            CacheRadios();
    }

    private void CacheRadios()
    {
        _keyCodes = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>()
            .ToFrozenDictionary(x => x.KeyCode);
    }

    public bool SendRadioMessage(EntityUid speaker, string message, RadioChannelPrototype radioChannel, [NotNullWhen(false)] out string? reason)
    {
        if (message.Length > MaxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length");

            return false;
        }

        if (TrySendInnateRadioMessage(speaker, message, radioChannel))
        {
            reason = null;

            return true;
        }

        if (!TryComp<HeadsetRadioableComponent>(speaker, out var comp))
        {
            reason = Loc.GetString("chat-system-radio-failed");

            return false;
        }

        if (!comp.Channels.Contains(radioChannel.ID))
        {
            reason = Loc.GetString("chat-system-radio-channel-failed", ("channel", radioChannel.ID));

            return false;
        }

        RaiseNetworkEvent(new AttemptHeadsetRadioEvent(GetNetEntity(speaker), message, radioChannel.ID));

        reason = null;

        return true;
    }

    private bool TrySendInnateRadioMessage(EntityUid speaker, string message, RadioChannelPrototype radioChannel)
    {
        if (!TryComp<InternalRadioComponent>(speaker, out var comp))
        {
            return false;
        }

        if (!comp.SendChannels.Contains(radioChannel.ID))
        {
            return false;
        }

        RaiseNetworkEvent(new AttemptInternalRadioEvent(GetNetEntity(speaker), message, radioChannel.ID));

        return true;
    }

    /// <summary>
    ///     Attempts to resolve radio prefixes in chat messages (e.g., remove a leading ":e" and resolve the requested
    ///     channel. Returns true if a radio message was attempted, even if the channel is invalid.
    /// </summary>
    /// <param name="source">Source of the message</param>
    /// <param name="input">The message to be modified</param>
    /// <param name="output">The modified message</param>
    /// <param name="channel">The channel that was requested, if any</param>
    /// <param name="quiet">Whether or not to generate an informative pop-up message.</param>
    /// <returns></returns>
    public bool TryProcessRadioMessage(
        EntityUid source,
        string input,
        out string output,
        out RadioChannelPrototype? channel,
        bool quiet = false)
    {
        output = input.Trim();
        channel = null;

        if (input.Length == 0)
            return false;

        if (input.StartsWith(RadioCommonPrefix))
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            channel = _prototypeManager.Index<RadioChannelPrototype>(CommonChannel);
            return true;
        }

        if (!(input.StartsWith(RadioChannelPrefix) || input.StartsWith(RadioChannelAltPrefix)))
            return false;

        if (input.Length < 2 || char.IsWhiteSpace(input[1]))
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("chat-system-no-radio-key"), source, source);
            return true;
        }

        var channelKey = input[1];
        channelKey = char.ToLower(channelKey);
        output = SanitizeMessageCapital(input[2..].TrimStart());

        if (channelKey == DefaultChannelKey)
        {
            var ev = new GetDefaultRadioChannelEvent();
            RaiseLocalEvent(source, ev);

            if (ev.Channel != null)
                _prototypeManager.TryIndex(ev.Channel, out channel);
            return true;
        }

        if (!_keyCodes.TryGetValue(channelKey, out channel) && !quiet)
        {
            var msg = Loc.GetString("chat-system-no-such-channel", ("key", channelKey));
            _popup.PopupEntity(msg, source, source);
        }

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to use the radio via a headset or similar device.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptHeadsetRadioEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;
    public readonly string Channel;

    public AttemptHeadsetRadioEvent(NetEntity speaker, string message, string channel)
    {
        Speaker = speaker;
        Message = message;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a mob tries to use the radio via their innate abilities.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptInternalRadioEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;
    public readonly string Channel;

    public AttemptInternalRadioEvent(NetEntity speaker, string message, string channel)
    {
        Speaker = speaker;
        Message = message;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public readonly string Channel;

    public RadioEvent(NetEntity speaker, string asName, string message, string channel)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a character has failed to speak on the radio.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public RadioFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}
