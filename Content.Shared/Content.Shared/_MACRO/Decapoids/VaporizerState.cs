using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Decapoids;

/// <summary>
/// Enum that contains the current indicator state, used for appearance data.
/// </summary>
[Serializable, NetSerializable]
public enum VaporizerVisuals : byte
{
    Indicator,
}
/// <summary>
/// Enum used to determine fill level of any given Vaporizer.
/// </summary>
[Serializable, NetSerializable]
public enum VaporizerState : byte
{
    Normal, // Vaporizer has the correct solution and is producing gas
    BadSolution, // Vaporizer has the incorrect solution
    LowSolution, // Vaporizer is low on the correct solution
    Empty, // Vaporizer has no solution
}
