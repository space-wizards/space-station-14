using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class SpaceVendorsUiState : BoundUserInterfaceState
{
    public List<AppraisedItem> AppraisedItems;

    public SpaceVendorsUiState(List<AppraisedItem> appraisedItems)
    {
        AppraisedItems = appraisedItems;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class AppraisedItem
{
    public readonly string Name;
    public readonly string Price;
    public readonly string Minutes;

    public AppraisedItem(string name, string price, string minutes)
    {
        Name = name;
        Price = price;
        Minutes = minutes;
    }
}
