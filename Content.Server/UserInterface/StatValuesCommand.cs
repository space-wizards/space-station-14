using System.Globalization;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.UserInterface;

[AdminCommand(AdminFlags.Debug)]
public sealed class StatValuesCommand : IConsoleCommand
{
    public string Command => "showvalues";
    public string Description => "Dumps all stats for a particular category into a table.";
    public string Help => $"{Command} <cargo>";
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
        var compFactory = IoCManager.Resolve<IComponentFactory>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        switch (args[0])
        {
            case "cargo":
                message = GetCargo(compFactory, protoManager);
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

    private StatValuesEuiMessage GetCargo( IComponentFactory compFactory, IPrototypeManager protoManager)
    {
        var values = new List<string[]>();

        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract ||
                !proto.Components.TryGetValue(compFactory.GetComponentName(typeof(MeleeWeaponComponent)),
                    out var meleeComp))
            {
                continue;
            }

            var comp = (MeleeWeaponComponent) meleeComp.Component;

            // TODO: Wielded damage
            // TODO: Esword damage

            values.Add(new[]
            {
                proto.ID,
                (comp.Damage.Total * comp.AttackRate).ToString(),
                comp.AttackRate.ToString(CultureInfo.CurrentCulture),
                comp.Damage.Total.ToString(),
                comp.Range.ToString(CultureInfo.CurrentCulture),
            });
        }

        var state = new StatValuesEuiMessage()
        {
            Headers = new List<string>()
            {
                "ID",
                "DPS",
                "Attack Rate",
                "Damage",
                "Range",
            },
            Values = values,
        };

        return state;
    }
}
