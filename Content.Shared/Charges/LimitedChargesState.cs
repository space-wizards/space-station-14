using Robust.Shared.Serialization;

namespace Content.Shared.Charges;

/// <summary>
///     Appearance data keys for entities with LimitedCharges.
/// </summary>
[Serializable, NetSerializable]
public enum LimitedChargesState : byte
{
    /// <summary>True if current charges > 0.</summary>
    HasCharges,
}
