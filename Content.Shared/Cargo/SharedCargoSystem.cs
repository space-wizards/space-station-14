using Content.Shared.Cargo.Orders;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

[NetSerializable, Serializable]
public enum CargoConsoleUiKey : byte
{
    Orders,
    Shuttle,
    Telepad
}

[NetSerializable, Serializable]
public enum CargoPalletConsoleUiKey : byte
{
    Sale
}

public abstract class SharedCargoSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <summary>
    /// Gets the name to display on order consoles
    /// </summary>
    /// <param name="order">The order to get the name for</param>
    /// <returns>The display name</returns>
    public string GetOrderDisplayName(CargoOrderData order)
    {
        if (GetOrderPrototype(order) is { } entityPrototype)
            return entityPrototype.Name;

        return Loc.GetString("cargo-console-menu-order-special-order");
    }

    /// <summary>
    /// Gets the prototype used for the order
    /// </summary>
    /// <param name="order">The order to get the prototype for</param>
    /// <returns>The prototype, null if there is no prototype</returns>
    public EntityPrototype? GetOrderPrototype(CargoOrderData order)
    {
        if (order is CargoOrderDataProduct orderDataProduct)
            return _protoMan.Index<EntityPrototype>(orderDataProduct.ProductId);

        return null;
    }

    /// <summary>
    /// Checks if an order is fulfillable
    /// </summary>
    /// <param name="order">The order to check</param>
    /// <returns>True if the order is fulfillable</returns>
    protected bool IsValidOrder(CargoOrderData order)
    {
        switch (order)
        {
            case CargoOrderDataProduct orderDataProduct:
                return _protoMan.HasIndex<EntityPrototype>(orderDataProduct.ProductId);
            case CargoOrderDataEntity orderDataEntity:
                return !Deleted(orderDataEntity.OrderEntity);
        }

        return false;
    }

    /// <summary>
    /// Fulfills the order by placing it at <paramref name="deliveryPoint"/>
    /// </summary>
    /// <param name="order">The order to fulfill</param>
    /// <param name="deliveryPoint">The location to fulfill the order to</param>
    /// <returns>The EntityUid of the order if it has been fulfilled, null if it was not</returns>
    protected EntityUid? FulfillOrder(CargoOrderData order, EntityCoordinates deliveryPoint)
    {
        switch (order)
        {
            case CargoOrderDataProduct orderDataProduct:
                return Spawn(orderDataProduct.ProductId, deliveryPoint);
            case CargoOrderDataEntity orderDataEntity:
                _transformSystem.SetCoordinates(orderDataEntity.OrderEntity, deliveryPoint);
                return orderDataEntity.OrderEntity;
        }

        return null;
    }
}

[Serializable, NetSerializable]
public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

[Serializable, NetSerializable]
public enum CargoTelepadVisuals : byte
{
    State,
};
