using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

[DataDefinition, NetSerializable, Serializable]
public sealed partial class CargoOrderContainerData
{
    /// <summary>
    /// The ID of the cargo crate of the container.
    /// </summary>
    [DataField]
    public string Container;

    /// <summary>
    /// The ID of the component which entities are inserted
    /// </summary>
    public string ContainerID = string.Empty;

    /// <summary>
    /// The max amount of items which can be spawned inside this container
    /// </summary>
    [DataField]
    public int MaxItems;

    /// <summary>
    /// The list of items which will be spawn in this container
    /// </summary>
    [DataField]
    public List<CargoOrderContainerSlot> Products = new();

    /// <summary>
    /// String to be added to the label of this container
    /// </summary>
    [DataField]
    public string LabelMessage = string.Empty;

    /// <summary>
    /// String of the name of the label of this container
    /// </summary>
    [DataField]
    public string LabelName = string.Empty;

    /// <summary>
    /// Whether or not this container represents a single spawn and will not spawn as a container
    /// </summary>
    public bool IsSingleProduct = false;

    /// <summary>
    /// Wheter or not the items must spawn in a crate an will not spawn in a parcel
    /// </summary>
    public bool CrateRequired = false;
    public int Cost;

    public CargoOrderContainerData(
        string container,
        string containerID,
        CargoOrderItemData? item = null,
        bool crateRequired = false,
        int maxItems = 30,
        int cost = 0
    )
    {
        Container = container;
        ContainerID = containerID;
        CrateRequired = crateRequired;
        MaxItems = maxItems;
        Cost = cost;
        // Item should only be null if only item in container and will not be spawned in a container
        if (item != null)
        {
            Products.Add(new CargoOrderContainerSlot(item, 1));
            IsSingleProduct = true;
        }
    }
}

[DataDefinition, NetSerializable, Serializable]
public sealed partial class CargoOrderContainerSlot
{
    // reference to original basket item
    public CargoOrderItemData Source;
    public int Quantity;

    public CargoOrderContainerSlot(CargoOrderItemData source, int quantity)
    {
        Source = source;
        Quantity = quantity;
    }
}
