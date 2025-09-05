using Robust.Shared.Audio;

namespace Content.Server.Disposal.Tube;

[RegisterComponent]
public sealed partial class DisposalTaggerComponent : DisposalTransitComponent
{
    [DataField]
    public string Tag = string.Empty;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
