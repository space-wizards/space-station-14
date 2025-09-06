using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisposalTaggerComponent : DisposalTransitComponent
{
    [DataField, AutoNetworkedField]
    public string Tag = string.Empty;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
