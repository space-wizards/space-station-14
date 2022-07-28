using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Headset;
using Content.Server.RadioKey.Components;
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

    private (string, int?) GetRadioPrefix(EntityUid source, string message)
    {
        var channelMessage = message.StartsWith(':') || message.StartsWith('.');
        var radioMessage = message.StartsWith(';') || channelMessage;
        if (!radioMessage) return (message, null);

        // Special case for empty messages
        if (message.Length <= 1)
            return (string.Empty, null);

        int? freq = null;
        var isDept = false;

        if (channelMessage && message.Length >= 2)
        {
            _keyCodes.TryGetValue(message[1], out var chan);

            if (chan == null)
            {
                // TODO handler for custom keys
                switch (message[1])
                {
                    case 'h':
                        // department (onefreq unlocked)
                        isDept = true;
                        break;
                    case 'b':
                        // MODE_BINARY
                        return (message, null);
                    case 'i':
                        // MODE_INTERCOM
                        return (message, null);
                    case 'r':
                        // MODE_R_HAND
                        return (message, null);
                    case 'l':
                        // MODE_L_HAND
                        return (message, null);
                    default:
                        _popup.PopupEntity(Loc.GetString("chat-manager-no-such-channel"), source, Filter.Entities(source));
                        break;
                }
            }

            freq = chan?.Frequency;

            // Strip message prefix.
            message = message[2..].TrimStart();
        }
        else
        {
            // Remove semicolon
            message = message[1..].TrimStart();
        }

        // for headsets using the `;` bind or it is an invalid freq
        if (_inventory.TryGetSlotEntity(source, "ears", out var entityUid) &&
            TryComp(entityUid, out HeadsetComponent? headset))
        {
            headset.RadioRequested = true;
            if (isDept && TryComp<RadioKeyComponent>(entityUid, out var radiokey))
            {
                freq = radiokey.UnlockedFrequency.First();
            }
            freq ??= headset.Frequency;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-headset-on-message"), source, Filter.Entities(source));
        }

        return (message, freq);
    }
}
