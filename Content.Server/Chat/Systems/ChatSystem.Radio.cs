using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Headset;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <summary>
    /// Cache of the keycodes for faster lookup.
    /// </summary>
    private Dictionary<char, RadioChannelPrototype> _keyCodes = new();

    private void InitializeRadio()
    {
        _prototypeManager.PrototypesReloaded += OnPrototypeReload;
        CacheRadios();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
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

    private void ShutdownRadio()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReload;
    }

    private (string, RadioChannelPrototype?) GetRadioPrefix(EntityUid source, string message)
    {
        // TODO: Turn common into a true frequency and support multiple aliases.
        var channelMessage = message.StartsWith(':') || message.StartsWith('.');
        var radioMessage = message.StartsWith(';') || channelMessage;
        if (!radioMessage) return (message, null);

        // Special case for empty messages
        if (message.Length <= 1)
            return (string.Empty, null);

        // Look for a prefix indicating a destination radio channel.
        RadioChannelPrototype? chan;
        if (channelMessage && message.Length >= 2)
        {
            _keyCodes.TryGetValue(message[1], out chan);

            if (chan == null)
            {
                _popup.PopupEntity(Loc.GetString("chat-manager-no-such-channel"), source, Filter.Entities(source));
                chan = null;
            }

            // Strip message prefix.
            message = message[2..].TrimStart();
        }
        else
        {
            // Remove semicolon
            message = message[1..].TrimStart();
            chan = _prototypeManager.Index<RadioChannelPrototype>("Common");
        }

        if (_inventory.TryGetSlotEntity(source, "ears", out var entityUid) &&
            TryComp(entityUid, out HeadsetComponent? headset))
        {
            headset.RadioRequested = true;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-headset-on-message"), source, Filter.Entities(source));
        }

        return (message, chan);
    }
}
