using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PdaRngCartridgeSystem))]
public sealed partial class PdaRngCartridgeComponent : Component
{
    /// <summary>Whether this PDA has already printed its one RNG ticket this round.</summary>
    [DataField, AutoNetworkedField]
    public bool HasRolled;

    [DataField]
    public EntProtoId PaperPrototype = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");
}
