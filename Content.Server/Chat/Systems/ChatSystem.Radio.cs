using System.Linq;
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
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs obj)
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

    private (string, RadioChannelPrototype?) RadioPrefix(EntityUid source, string message)
    {
        var channelMessage = message.StartsWith(':') || message.StartsWith('.');
        var radioMessage = message.StartsWith(';') || channelMessage;
        if (!radioMessage) return (message, null);

        // Special case for empty messages
        if (message.Length <= 1)
            return ("", null);

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
            var parts = message.Split(' ').ToList();
            parts.RemoveAt(0);
            message = string.Join(" ", parts);
        }
        else
        {
            // Remove semicolon
            message = message[1..].TrimStart();
            chan = null;
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
