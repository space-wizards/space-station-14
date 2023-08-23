using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Conveyor;

[RegisterComponent, NetworkedComponent]
public sealed partial class ConveyorComponent : Component
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

    [DataField("forwardPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ForwardPort = "Forward";

    [DataField("reversePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ReversePort = "Reverse";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    [ViewVariables]
    public readonly HashSet<EntityUid> Intersecting = new();
}

[Serializable, NetSerializable]
public sealed class ConveyorComponentState : ComponentState
{
    public bool Powered;
    public Angle Angle;
    public float Speed;
    public ConveyorState State;

    public ConveyorComponentState(Angle angle, float speed, ConveyorState state, bool powered)
    {
        Angle = angle;
        Speed = speed;
        State = state;
        Powered = powered;
    }
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

