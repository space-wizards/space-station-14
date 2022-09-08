using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.UserInterface;

[AdminCommand(AdminFlags.Debug)]
public sealed class StatValuesCommand : IConsoleCommand
{
    public string Command => "showvalues";
    public string Description => "Dumps all stats for a particular category into a table.";
    public string Help => $"{Command} <cargosell>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession pSession)
        {
            shell.WriteError($"{Command} can't be run on server!");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError($"Invalid number of args, need 1");
            return;
        }

        StatValuesEuiMessage message;

        switch (args[0])
        {
            case "cargosell":
                message = GetCargo();
                break;
            default:
                shell.WriteError($"{args[0]} is not a valid stat!");
                return;
        }

        var euiManager = IoCManager.Resolve<EuiManager>();
        var eui = new StatValuesEui();
        euiManager.OpenEui(eui, pSession);
        eui.SendMessage(message);
    }

    private StatValuesEuiMessage GetCargo()
    {
        // Okay so there's no easy way to do this with how pricing works
        // So we'll just get the first value for each prototype ID which is probably good enough for the majority.

        var values = new List<string[]>();
        var entManager = IoCManager.Resolve<IEntityManager>();
        var priceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PricingSystem>();
        var metaQuery = entManager.GetEntityQuery<MetaDataComponent>();
        var prices = new HashSet<string>(256);

        foreach (var entity in entManager.GetEntities())
        {
            if (!metaQuery.TryGetComponent(entity, out var meta))
                continue;

            var id = meta.EntityPrototype?.ID;

            // We'll add it even if we don't have it so we don't have to raise the event again because this is probably faster.
            if (id == null || !prices.Add(id))
                continue;

            var price = priceSystem.GetPrice(entity);

            if (price == 0)
                continue;

            values.Add(new string[]
            {
                id,
                $"{price:0}",
            });
        }

        var state = new StatValuesEuiMessage()
        {
            Title = "Cargo sell prices",
            Headers = new List<string>()
            {
                "ID",
                "Price",
            },
            Values = values,
        };

        return state;
    }
}
