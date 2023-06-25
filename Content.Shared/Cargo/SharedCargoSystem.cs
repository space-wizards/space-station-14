using Content.Shared.Cargo.Orders;
using JetBrains.Annotations;
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

    public string GetOrderDisplayName(CargoOrderData order)
    {
        if (GetOrderPrototype(order) is { } entityPrototype)
            return entityPrototype.Name;

        return Loc.GetString("cargo-console-menu-order-special-order");
    }

    public EntityPrototype? GetOrderPrototype(CargoOrderData order)
    {
        if (order is CargoOrderDataProduct orderDataProduct)
            return _protoMan.Index<EntityPrototype>(orderDataProduct.ProductId);

        return null;
    }

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
