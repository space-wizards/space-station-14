using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDisposalTubeSystem))]
public sealed partial class DisposalRouterComponent : DisposalTransitComponent
{
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags = new();

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
