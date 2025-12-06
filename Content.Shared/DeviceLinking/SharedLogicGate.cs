using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;


/// <summary>
/// Types of logic gates that can be used, determines how the output port is set.
/// </summary>
[Serializable, NetSerializable]
public enum LogicGate : byte
{
    Or,
    And,
    Xor,
    Nor,
    Nand,
    Xnor
}

/// <summary>
/// Tells clients which logic gate layer to draw.
/// </summary>
[Serializable, NetSerializable]
public enum LogicGateVisuals : byte
{
    Gate,
    InputA,
    InputB,
    Output
}

/// <summary>
/// Sprite layer for the logic gate.
/// </summary>
[Serializable, NetSerializable]
public enum LogicGateLayers : byte
{
    Gate,
    InputA,
    InputB,
    Output
}

/// <summary>
/// The possible states of a logic-capable signal.
/// Stored in network payload data of device network messages.
/// </summary>
[Serializable, NetSerializable]
public enum SignalState : byte
{
    Momentary, // Instantaneous pulse high, compatibility behavior
    Low,
    High
}
