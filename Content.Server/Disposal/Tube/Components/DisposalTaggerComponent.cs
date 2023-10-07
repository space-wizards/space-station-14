using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Robust.Shared.Audio;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent]
public sealed partial class DisposalTaggerComponent : DisposalTransitComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Tag = string.Empty;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
