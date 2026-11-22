using Content.Shared.Cargo;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.UI;

public sealed partial class OrderLogListing : BoxContainer
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly SpriteSystem _sprite;

    public OrderLogListing()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
    }

    public void SetOrders(CargoOrderData[] orders)
    {
        RemoveAllChildren();

        for (var i = 0; i < orders.Length; i++)
        {
            var order = orders[i];
            if (!_prototypeManager.TryIndex(order.Product, out var product))
                continue;

            if (!_prototypeManager.TryIndex(order.Account, out var account))
                continue;

            var accountCode = Loc.GetString(account.Code);
            var accountColor = account.Color;

            var reason = string.IsNullOrWhiteSpace(order.Reason)
                ? string.Empty
                : Loc.GetString("order-log-reason",
                    ("hasReason", true),
                    ("reason", order.Reason));

            var entry = new OrderLogEntry();
            entry.AddOrder(
                _sprite.Frame0(product.Icon),
                $"{product.Name} x{order.OrderQuantity}",
                accountCode,
                accountColor,
                order.Requester,
                reason,
                Loc.GetString("cargo-console-menu-points-amount",
                    ("amount", (order.OrderQuantity * product.Cost).ToString())),
                striped: i % 2 == 1);

            AddChild(entry);
        }
    }
}
