using Robust.Shared.Audio;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent, Access(typeof(DisposalTubeSystem))]
public sealed partial class DisposalRouterComponent : DisposalJunctionComponent
{
    [DataField]
    public HashSet<string> Tags = new();

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
