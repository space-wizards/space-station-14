using Content.Shared.MachineLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Conveyor;

[RegisterComponent, NetworkedComponent]
public sealed class ConveyorComponent : Component
{
    /// <summary>
    ///     The angle to move entities by in relation to the owner's rotation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("angle")]
    public Angle Angle = Angle.Zero;

    /// <summary>
    ///     The amount of units to move the entity by per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("speed")]
    public float Speed = 2f;

    /// <summary>
    ///     The current state of this conveyor
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ConveyorState State;

    [ViewVariables]
    public bool Powered;

    [DataField("forwardPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
    public string ForwardPort = "Forward";

    [DataField("reversePort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string ReversePort = "Reverse";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string OffPort = "Off";

    [ViewVariables]
    public readonly HashSet<EntityUid> Intersecting = new();
}

[Serializable, NetSerializable]
public enum ConveyorVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum ConveyorState : byte
{
    Off,
    Forward,
    Reverse
}

