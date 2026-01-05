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

[Serializable, NetSerializable]
public enum MindState : byte
{
    None,
    Dead,
    Catatonic,
    SSD,
    DeadSSD,
    Irrecoverable
}
