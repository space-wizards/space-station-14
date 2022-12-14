using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Radio.Components;
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

    // Dict so we can look up the keycode using channel names later
    private Dictionary<string, char> _channelNames = new();
    private char _defaultChannelCode = '\0';
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
        _channelNames.Clear();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>())
        {
            _keyCodes.Add(proto.KeyCode, proto);
            _channelNames.Add(proto.ID, proto.KeyCode);
        }


    }

    private void ShutdownRadio()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReload;
    }

    private (string, RadioChannelPrototype?) GetRadioPrefix(EntityUid source, string message)
    {
        // TODO: Turn common into a true frequency and support multiple aliases.
        var isRadioMessage = false;
        RadioChannelPrototype? channel = null;

        // If message starts with :h, then replace keycode in message with defaultChannel's keycode
        var locChannelMessage = message.StartsWith(":h") || message.StartsWith(".h");
        if (locChannelMessage && message.Length >= 2)
        {
            if (_inventory.TryGetSlotEntity(source, "ears", out var _headsetUid) && TryComp<HeadsetComponent>(_headsetUid, out var _headsetComponent) && _headsetComponent.defaultChannel != null)
            {
                //Remove :h prefix 
                message = message[2..].TrimStart();
                Logger.Info("Message with no h: " + message);

                // Add keycode for specific Channel, looked up in _channelNames from earlier
                _defaultChannelCode = _channelNames[_headsetComponent.defaultChannel];
                Logger.Info("Default Channel Code Lookup: " + _defaultChannelCode);

                // Needs special code to handle Common channel because otherwise it would end up like ";:"
                if (_defaultChannelCode == ';')
                {
                    message = "; " + message;
                }
                else
                {
                    message = ":" + _defaultChannelCode + " " + message;
                    Logger.Info("Replaced :h with channel code: " + _defaultChannelCode);
                    Logger.Info("New modified message: " + message);
                }

            }
        }



        // First check if this is a message to the base radio frequency
        if (message.StartsWith(';'))
        {
            // First Remove semicolon
            channel = _prototypeManager.Index<RadioChannelPrototype>("Common");
            message = message[1..].TrimStart();
            isRadioMessage = true;
        }
        Logger.Info("message being parsed like normal now: " + message);
        // Check now if the remaining message is a targeted radio message
        if ((message.StartsWith(':') || message.StartsWith('.')) && message.Length >= 2)
        {
            Logger.Info("message recognized as being radio");
            // Strip remaining message prefix.
            _keyCodes.TryGetValue(message[1], out channel);
            message = message[2..].TrimStart();
            isRadioMessage = true;
        }

        // If not a radio message at all
        if (!isRadioMessage) return (message, null);

        // Special case for empty messages
        if (message.Length <= 1)
            return (string.Empty, null);

        if (channel == null)
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-such-channel"), source, Filter.Entities(source));
            channel = null;
        }

        // Re-capitalize message since we removed the prefix.
        message = SanitizeMessageCapital(message);

        var hasHeadset = _inventory.TryGetSlotEntity(source, "ears", out var entityUid)  && HasComp<HeadsetComponent>(entityUid);

        if (!hasHeadset && !HasComp<IntrinsicRadioTransmitterComponent>(source))
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-headset-on-message"), source, Filter.Entities(source));
        }

        return (message, channel);
    }
}
