using Content.Shared.Nodes.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nodes.Debugging;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DebugNodeLinkerComponent : Component
{
    /// <summary>
    /// Whether or not this item is currently linking or unlinking graph nodes.
    /// </summary>
    [AutoNetworkedField]
    [DataField("mode")]
    [ViewVariables(VVAccess.ReadWrite)]
    public DebugLinkerMode Mode = DebugLinkerMode.Link;

    /// <summary>
    /// The node that this linker is attempting to link/unlink from another node.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables]
    public EntityUid? Node = null;

    /// <summary>
    /// The flags used when manually creating an edge between nodes.
    /// </summary>
    [AutoNetworkedField]
    [DataField("edgeFlags")]
    [ViewVariables]
    public EdgeFlags Flags = EdgeFlags.None;

    /// <summary>
    /// The sound played when switching the linker mode.
    /// </summary>
    [DataField("modeSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ModeSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

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

    /// <summary>
    /// The sound played when prompting a node to update its automatic edges.
    /// </summary>
    [DataField("updateSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier UpdateSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// The sound played when failing to link or unlink two nodes.
    /// </summary>
    [DataField("failSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}

public enum DebugLinkerMode : byte
{
    Link,
    Unlink,
    Update,
}

public static class DebugLinkerHelpers
{
    public static DebugLinkerMode Next(this DebugLinkerMode mode)
    {
        return mode switch
        {
            DebugLinkerMode.Link => DebugLinkerMode.Unlink,
            DebugLinkerMode.Unlink => DebugLinkerMode.Update,
            DebugLinkerMode.Update => DebugLinkerMode.Link,
            _ => DebugLinkerMode.Link,
        };
    }
}
