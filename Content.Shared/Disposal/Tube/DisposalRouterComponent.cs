using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Disposal pipes with this component can route entities in different directions
/// depending on the tags of the disposal holder passing through it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDisposalTubeSystem))]
public sealed partial class DisposalRouterComponent : DisposalTransitComponent
{
    /// <summary>
    /// List of tags that will be compared against the tags possessed
    /// by any disposal holders that pass by. If the tag lists overlap,
    /// the disposal holder will be routed via the first exit, if possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags = new();

    /// <summary>
    /// Sound played when <see cref="Tags"/> is updated by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
