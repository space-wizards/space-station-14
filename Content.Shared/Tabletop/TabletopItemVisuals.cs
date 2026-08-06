using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop;

/// <summary>
/// An enum used for appearance keys for tabletop pieces.
/// </summary>
[Serializable, NetSerializable]
public enum TabletopItemVisuals : byte
{
    /// <summary>The prototype that this piece should mimic, if any.</summary>
    Prototype,
}
