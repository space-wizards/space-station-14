using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract class SharedChatSystem : EntitySystem
{
    public const char RadioCommonPrefix = ';';
    public const char RadioChannelPrefix = ':';
    public const char RadioChannelAltPrefix = '.';
    public const char LocalPrefix = '>';
    public const char ConsolePrefix = '/';
    public const char DeadPrefix = '\\';
    public const char LOOCPrefix = '(';
    public const char OOCPrefix = '[';
    public const char EmotesPrefix = '@';
    public const char EmotesAltPrefix = '*';
    public const char AdminPrefix = ']';
    public const char WhisperPrefix = ',';
    public const char DefaultChannelKey = 'h';

    [ValidatePrototypeId<RadioChannelPrototype>]
    public const string CommonChannel = "Common";

    public static string DefaultChannelPrefix = $"{RadioChannelPrefix}{DefaultChannelKey}";

    [ValidatePrototypeId<SpeechVerbPrototype>]
    public const string DefaultSpeechVerb = "Default";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Cache of the keycodes for faster lookup.
    /// </summary>
    private Dictionary<char, RadioChannelPrototype> _keyCodes = new();

    public override void Initialize()
    {
        base.Initialize();
        DebugTools.Assert(_prototypeManager.HasIndex<RadioChannelPrototype>(CommonChannel));
        _prototypeManager.PrototypesReloaded += OnPrototypeReload;
        CacheRadios();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.ByType.ContainsKey(typeof(RadioChannelPrototype)))
            CacheRadios();
    }

    private void CacheRadios()
    {
        _keyCodes.Clear();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>())
        {
            _keyCodes.Add(proto.KeyCode, proto);
        }
    }

    public override void Shutdown()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReload;
    }

    /// <summary>
    ///     Attempts to find an applicable <see cref="SpeechVerbPrototype"/> for a speaking entity's message.
    ///     If one is not found, returns <see cref="DefaultSpeechVerb"/>.
    /// </summary>
    public SpeechVerbPrototype GetSpeechVerb(EntityUid source, string message, SpeechComponent? speech = null)
    {
        if (!Resolve(source, ref speech, false))
            return _prototypeManager.Index<SpeechVerbPrototype>(DefaultSpeechVerb);

        // check for a suffix-applicable speech verb
        SpeechVerbPrototype? current = null;
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototypeManager.Index<SpeechVerbPrototype>(id);
            if (message.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        return current ?? _prototypeManager.Index<SpeechVerbPrototype>(speech.SpeechVerb);
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
    public bool TryProccessRadioMessage(
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
                _popup.PopupEntity(Loc.GetString("chat-manager-no-radio-key"), source, source);
            return true;
        }

        var channelKey = input[1];
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
            var msg = Loc.GetString("chat-manager-no-such-channel", ("key", channelKey));
            _popup.PopupEntity(msg, source, source);
        }

        return true;
    }

    public string SanitizeMessageCapital(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Capitalize first letter
        message = char.ToUpper(message[0]) + message.Remove(0, 1);
        return message;
    }

    public string SanitizeMessageCapitalizeTheWordI(string message, string theWordI = "i")
    {
        if (string.IsNullOrEmpty(message))
            return message;

        for
        (
            var index = message.IndexOf(theWordI);
            index != -1;
            index = message.IndexOf(theWordI, index + 1)
        )
        {
            // Stops the code If It's tryIng to capItalIze the letter I In the mIddle of words
            // Repeating the code twice is the simplest option
            if (index + 1 < message.Length && char.IsLetter(message[index + 1]))
                continue;
            if (index - 1 >= 0 && char.IsLetter(message[index - 1]))
                continue;

            var beforeTarget = message.Substring(0, index);
            var target = message.Substring(index, theWordI.Length);
            var afterTarget = message.Substring(index + theWordI.Length);

            message = beforeTarget + target.ToUpper() + afterTarget;
        }

        return message;
    }
}
