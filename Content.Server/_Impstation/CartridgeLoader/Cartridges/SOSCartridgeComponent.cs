using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class SOSCartridgeComponent : Component
{
    [DataField]
    public const string PDAIdContainer = "PDA-id";

    [DataField]
    public string DefaultName = "sos-caller-defaultname";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDefaultName => Loc.GetString(DefaultName);

    [DataField]
    public string HelpMessage = "sos-message";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedHelpMessage => Loc.GetString(HelpMessage);

    [DataField]
    public ProtoId<RadioChannelPrototype> HelpChannel = "Security";
}
