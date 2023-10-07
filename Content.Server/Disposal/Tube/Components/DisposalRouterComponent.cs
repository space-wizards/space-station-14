using Content.Server.Disposal.Tube.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Disposal.Tube.Components;

/// <summary>
/// Routes contents to the side if they contain at least one tag specified.
/// Goes straight ahead if not.
/// </summary>
[RegisterComponent, Access(typeof(DisposalRouterSystem))]
public sealed partial class DisposalRouterComponent : Component
{
    [DataField]
    public HashSet<string> Tags = new();

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };
}
