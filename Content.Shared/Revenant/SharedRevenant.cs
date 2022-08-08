using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Revenant;

public sealed class RevenantShopActionEvent : InstantActionEvent { }
public sealed class RevenantDefileActionEvent : InstantActionEvent { }
public sealed class RevenantOverloadLightsActionEvent : InstantActionEvent { }
public sealed class RevenantBlightActionEvent : InstantActionEvent { }
public sealed class RevenantMalfunctionActionEvent : InstantActionEvent { }

[NetSerializable, Serializable]
public enum RevenantVisuals : byte
{
    Corporeal,
    Stunned,
    Harvesting,
}

[NetSerializable, Serializable]
public enum RevenantUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class RevenantUpdateState : BoundUserInterfaceState
{
    public float Essence;

    public readonly List<RevenantStoreListingPrototype> Listings;

    public RevenantUpdateState(float essence, List<RevenantStoreListingPrototype> listings)
    {
        Essence = essence;
        Listings = listings;
    }
}

[Serializable, NetSerializable]
public sealed class RevenantBuyListingMessage : BoundUserInterfaceMessage
{
    public RevenantStoreListingPrototype Listing;

    public RevenantBuyListingMessage (RevenantStoreListingPrototype listing)
    {
        Listing = listing;
    }
}
