using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Conduit.Router;

/// <summary>
/// Attached to conduit that can direct entities out different
/// exits depending on what tags they both possess.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ConduitRouterSystem))]
public sealed partial class ConduitRouterComponent : Component
{
    /// <summary>
    /// List of tags that will be compared against the tags possessed
    /// by any entities that pass by. If the tag lists overlap,
    /// the entity will be routed towards the first exit, if possible.
    /// Otherwise it will be routed towards the second exit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags = new();

    /// <summary>
    /// Sound played when <see cref="Tags"/> is updated by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}

