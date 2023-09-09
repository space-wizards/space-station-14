using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nodes.Debugging;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DebugNodeLinkerComponent : Component
{
    /// <summary>
    /// The node that this linker is attempting to link/unlink from another node.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables]
    public EntityUid? Node = null;

    /// <summary>
    /// Whether or not this item is currently linking or unlinking graph nodes.
    /// </summary>
    [AutoNetworkedField]
    [DataField("mode")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Mode = true;

    /// <summary>
    /// The sound played when selecting an initial node to link or unlink.
    /// </summary>
    [DataField("markSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier MarkSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// The sound played when selecting an initial node to link or unlink.
    /// </summary>
    [DataField("clearSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ClearSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// The sound played when creating an edge between two nodes.
    /// </summary>
    [DataField("linkSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// The sound played when removing an edge between two nodes.
    /// </summary>
    [DataField("unlinkSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier UnlinkSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
