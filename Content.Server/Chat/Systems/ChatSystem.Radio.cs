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
        var isRadioMessage = false;
        RadioChannelPrototype? channel = null;

        // Check if have headset and grab headset UID for later
        var hasHeadset = _inventory.TryGetSlotEntity(source, "ears", out var entityUid) & TryComp<HeadsetComponent>(entityUid, out var _headsetComponent);

        // First check if this is a message to the base radio frequency
        if (message.StartsWith(';'))
        {
            // First Remove semicolon
            channel = _prototypeManager.Index<RadioChannelPrototype>("Common");
            message = message[1..].TrimStart();
            isRadioMessage = true;
        }


        // Check now if the remaining message is a radio message
        if ((message.StartsWith(':') || message.StartsWith('.')) && message.Length >= 2)
        {
            // Redirect to defaultChannel of headsetComp if it goes to "h" channel code after making sure defaultChannel exists
            if (message[1] == 'h'
                && _headsetComponent != null
                && _headsetComponent.defaultChannel != null
                && _prototypeManager.TryIndex(_headsetComponent.defaultChannel, out RadioChannelPrototype? protoDefaultChannel))
            {
                // Set Channel to headset defaultChannel
                channel = protoDefaultChannel;
            }
            else // otherwise it's a normal, targeted channel keycode
            {
                _keyCodes.TryGetValue(message[1], out channel);
            }

            // Strip remaining message prefix.
            message = message[2..].TrimStart();
            isRadioMessage = true;
        }

        // If not a radio message at all
        if (!isRadioMessage) return (message, null);

        // Special case for empty messages
        if (message.Length <= 1)
            return (string.Empty, null);

        // Check for headset before no-such-channel, otherwise you can get two PopupEntities if no headset and no channel
        if (hasHeadset & channel == null )
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-such-channel"), source, source);
            channel = null;
        }

        // Re-capitalize message since we removed the prefix.
        message = SanitizeMessageCapital(message);

        

        if (!hasHeadset && !HasComp<IntrinsicRadioTransmitterComponent>(source))
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-headset-on-message"), source, source);
        }

        return (message, channel);
    }
}
