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
    //Dict of Headset IDs and what their "defaults" are
    //probably should be put into the yml files themselves down the line
    private Dictionary<string, string> headsetCommonDict = new()
            {
                { "ClothingHeadsetQM", ":u "},
                { "ClothingHeadsetCargo", ":u "},
                { "ClothingHeadsetAltCargo", ":u "},
                { "ClothingHeadsetMining", ":u "},
                { "ClothingHeadsetCentCom", ":y "},
                { "ClothingHeadsetAltCentCom", ":y "},
                { "ClothingHeadsetCommand", ":c "},
                { "ClothingHeadsetAltCommand", ":c "},
                { "ClothingHeadsetCE", ":e "},
                { "ClothingHeadsetEngineering", ":e "},
                { "ClothingHeadsetAltEngineering", ":e "},
                { "ClothingHeadsetMedical", ":m "},
                { "ClothingHeadsetAltMedical", ":m "},
                { "ClothingHeadsetMedicalScience", ":m "},
                { "ClothingHeadsetRD", ":n "},
                { "ClothingHeadsetRobotics", ":n "},
                { "ClothingHeadsetScience", ":n "},
                { "ClothingHeadsetAltScience", ":n "},
                { "ClothingHeadsetSecurity", ":s "},
                { "ClothingHeadsetAltSecurity", ":s "},
                { "ClothingHeadsetService", ":v "},
                { "ClothingHeadsetAltSyndicate", ":t " },
            };
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

        // Replaces :h with whatevs the headset's "default" is
        var locChannelMessage = message.StartsWith(":h") || message.StartsWith(".h");
        if (locChannelMessage && message.Length >= 2)
        {
            if (_inventory.TryGetSlotEntity(source, "ears", out var headsetUid) && HasComp<HeadsetComponent>(headsetUid))
            {
                message = message[2..].TrimStart();
                var headsetUid2 = headsetUid ?? default(EntityUid);
                var entityPrototype = Prototype(headsetUid2) ?? default(EntityPrototype);
                if (entityPrototype != null)
                {
                    var entityId = entityPrototype.ID;
                    message = headsetCommonDict[entityId] + message;
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
