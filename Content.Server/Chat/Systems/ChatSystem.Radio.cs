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
        // First check if this is a message to the base radio frequency
        if (message.StartsWith(';'))
        {
            // First Remove semicolon
            channel = _prototypeManager.Index<RadioChannelPrototype>("Common");
            message = message[1..].TrimStart();
            isRadioMessage = true;
        }

        // Check now if the remaining message is a targeted radio message
        if ((message.StartsWith(':') || message.StartsWith('.')) && message.Length >= 2)
        {
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
            _popup.PopupEntity(Loc.GetString("chat-manager-no-such-channel"), source, source);
            channel = null;
        }

        // Re-capitalize message since we removed the prefix.
        message = SanitizeMessageCapital(message);

        var hasHeadset = _inventory.TryGetSlotEntity(source, "ears", out var entityUid)  && HasComp<HeadsetComponent>(entityUid);

        if (!hasHeadset && !HasComp<IntrinsicRadioTransmitterComponent>(source))
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-no-headset-on-message"), source, source);
        }

        return (message, channel);
    }
}
