using Content.Server.Chat.Systems;
using Content.Shared.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech.Components;
using Content.Server.Wires;
using Content.Shared.Wires;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech;

public sealed partial class ListenWireAction : BaseToggleWireAction
{
    private WiresSystem _wires = default!;
    private ChatSystem _chat = default!;
    private RadioSystem _radio = default!;
    private IPrototypeManager _protoMan = default!;

    /// <summary>
    /// Length of the gibberish string sent when pulsing the wire
    /// </summary>
    private const int NoiseLength = 16;
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-listen";

    public override object? StatusKey { get; } = ListenWireActionKey.StatusKey;

    public override object? TimeoutKey { get; } = ListenWireActionKey.TimeoutKey;

    public override int Delay { get; } = 10;

    public override void Initialize()
    {
        base.Initialize();

        _wires = EntityManager.System<WiresSystem>();
        _chat = EntityManager.System<ChatSystem>();
        _radio = EntityManager.System<RadioSystem>();
        _protoMan = IoCManager.Resolve<IPrototypeManager>();
    }
    public override StatusLightState? GetLightState(Wire wire)
    {
        if (GetValue(wire.Owner))
            return StatusLightState.On;
        else
        {
            if (TimeoutKey != null && _wires.HasData(wire.Owner, TimeoutKey))
                return StatusLightState.BlinkingSlow;
            return StatusLightState.Off;
        }
    }
    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (setting)
        {
            // If we defer removal, the status light gets out of sync
            EntityManager.RemoveComponent<BlockListeningComponent>(owner);
        }
        else
        {
            EntityManager.EnsureComponent<BlockListeningComponent>(owner);
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return !EntityManager.HasComponent<BlockListeningComponent>(owner);
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        if (!GetValue(wire.Owner) || !IsPowered(wire.Owner))
            return;

        var chars = Loc.GetString("wire-listen-pulse-characters").ToCharArray();
        var noiseMsg = _chat.BuildGibberishString(chars, NoiseLength);

        if (!EntityManager.TryGetComponent<RadioMicrophoneComponent>(wire.Owner, out var radioMicroPhoneComp))
            return;

        if (!EntityManager.TryGetComponent<VoiceOverrideComponent>(wire.Owner, out var voiceOverrideComp))
            return;

        // The reason for the override is to make the voice sound like its coming from electrity rather than the intercom.
        voiceOverrideComp.NameOverride = Loc.GetString("wire-listen-pulse-identifier");
        voiceOverrideComp.Enabled = true;
        _radio.SendRadioMessage(wire.Owner, noiseMsg, _protoMan.Index<RadioChannelPrototype>(radioMicroPhoneComp.BroadcastChannel), wire.Owner);
        voiceOverrideComp.Enabled = false;

        base.Pulse(user, wire);
    }
}
