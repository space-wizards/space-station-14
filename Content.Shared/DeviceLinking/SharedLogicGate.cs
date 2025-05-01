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
