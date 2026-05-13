using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.Magnet;

/// <summary>
/// Claim an offer from the magnet UI.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MagnetClaimOfferEvent : BoundUserInterfaceMessage
{
    public int Index;
}

