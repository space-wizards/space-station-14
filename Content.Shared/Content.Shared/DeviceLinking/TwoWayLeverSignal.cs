using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// Appearance data keys used by a two-way lever.
/// </summary>
[Serializable, NetSerializable]
public enum TwoWayLeverVisuals : byte
{
    State
}

/// <summary>
/// Possible positions of a two-way lever.
/// </summary>
[Serializable, NetSerializable]
public enum TwoWayLeverState : byte
{
    Middle,
    Right,
    Left
}
