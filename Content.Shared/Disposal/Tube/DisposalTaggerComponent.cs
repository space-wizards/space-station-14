using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Disposal holders that pass through this pipe will be marked with the tag
/// specified by <see cref="Tag"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisposalTaggerComponent : DisposalTransitComponent
{
    /// <summary>
    /// Tag to apply to passing entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Tag = string.Empty;

    /// <summary>
    /// Sound played when <see cref="Tag"/> is changed by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
