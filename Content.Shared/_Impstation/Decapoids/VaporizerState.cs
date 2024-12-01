using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Decapoids;

[Serializable, NetSerializable]
public enum VaporizerVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum VaporizerVisualLayers : byte
{
    Indicator,
}

[Serializable, NetSerializable]
public enum VaporizerState : byte
{
    Normal, // Vaporizer has the correct solution and is producing gas
    BadSolution, // Vaporizer has the incorrect solution
    LowSolution, // Vaporizer is low on the correct solution
    Empty, // Vaporizer has no solution
}
