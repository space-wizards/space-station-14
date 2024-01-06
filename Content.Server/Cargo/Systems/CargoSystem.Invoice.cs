using Content.Shared.Cargo;
using Robust.Shared.Map;
using Content.Server.Paper;
using Content.Server.Cargo.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private EntityUid GenerateInvoice(CargoOrderData order, EntityCoordinates spawnLocation, string paperPrototype)
    {
        // get the order prototype to get the order name for
        var orderPrototype = _prototypeManager.Index(order.ProductId);

        // Create a sheet of paper to write the order details on
        var printed = EntityManager.SpawnEntity(paperPrototype, spawnLocation);
        if (TryComp<PaperComponent>(printed, out var paper))
        {
            // fill in the order data
            var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
            _metaSystem.SetEntityName(printed, val);

            _paperSystem.SetContent(printed, Loc.GetString(
                    "cargo-console-paper-print-text",
                    ("orderNumber", order.OrderId),
                    ("itemName", orderPrototype.Name),
                    ("requester", order.Requester),
                    ("reason", order.Reason),
                    ("approver", order.Approver ?? string.Empty)),
                paper);
        }

        // set the OrderId of the invoice
        if (TryComp<CargoInvoiceComponent>(printed, out var invoice))
        {
            invoice.OrderId = order.OrderId;
        }

        return printed;
    }
}
