using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Speech;
using Content.Shared.Wires;

namespace Content.Server.Speech;

public sealed partial class SpeechWireAction : ComponentWireAction<SpeechComponent>
{
    private SpeechSystem _speech = default!;
    private PopupSystem _popup = default!;

    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-speech";

    public override object? StatusKey { get; } = SpeechWireActionKey.StatusKey;

    public override StatusLightState? GetLightState(Wire wire, SpeechComponent component)
        => component.Enabled ? StatusLightState.On : StatusLightState.Off;

    public override void Initialize()
    {
        base.Initialize();

        _speech = EntityManager.System<SpeechSystem>();
        _popup = EntityManager.System<PopupSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, SpeechComponent component)
    {
        _speech.SetSpeech(wire.Owner, false, component);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SpeechComponent component)
    {
        _speech.SetSpeech(wire.Owner, true, component);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SpeechComponent component)
    {
        _popup.PopupEntity(Loc.GetString("wire-speech-pulse", ("name", wire.Owner)), wire.Owner);
    }
}
