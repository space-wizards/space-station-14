using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiia.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaScramblerComponent : Component
{
    [DataField]
    public EntityUid? ScramblerAction;

    [DataField]
    public string? ScramblerActionId = "ActionKodepiiaScrambler";

    [DataField]
    public SoundSpecifier ScramblerSound = new SoundPathSpecifier("/Audio/_Impstation/Kodepiia/kodescramble/kodescramble.ogg");

}
