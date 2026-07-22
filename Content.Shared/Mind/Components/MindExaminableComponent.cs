using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mind.Components;

/// <summary>
/// This component adds an examine text to the owner entity based on the state of their mind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MindExamineSystem))]
public sealed partial class MindExaminableComponent : Component
{
    /// <summary>
    /// The state the mind is currently in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MindState State = MindState.None;
}

/// <summary>
/// The states for when an entity with a mind is examined.
/// </summary>
[Serializable, NetSerializable]
public enum MindState : byte
{
    None, // No text
    Dead, // Player is dead but still connected
    Catatonic, // Entity is alive but has no mind attached to it.
    SSD, // Player disconnected while alive
    DeadSSD, // Player died and disconnected
    Irrecoverable // Entity is dead and has no mind attached
}
