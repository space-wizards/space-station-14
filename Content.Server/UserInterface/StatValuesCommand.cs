using System.Globalization;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Server.EUI;
using Content.Server.Item;
using Content.Shared.Administration;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.UserInterface;

[AdminCommand(AdminFlags.Debug)]
public sealed class StatValuesCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public string Command => "showvalues";
    public string Description => Loc.GetString("stat-values-desc");
    public string Help => $"{Command} <cargosell / lathesell / melee / itemsize>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } pSession)
        {
            shell.WriteError(Loc.GetString("stat-values-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("stat-values-args"));
            return;
        }

        StatValuesEuiMessage message;

        switch (args[0])
        {
            case "cargosell":
                message = GetCargo();
                break;
            case "lathesell":
                message = GetLatheMessage();
                break;
            case "melee":
                message = GetMelee();
                break;
            case "itemsize":
                message = GetItem();
                break;
            default:
                shell.WriteError(Loc.GetString("stat-values-invalid", ("arg", args[0])));
                return;
        }

        var eui = new StatValuesEui();
        _eui.OpenEui(eui, pSession);
        eui.SendMessage(message);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromOptions(new[] { "cargosell", "lathesell", "melee" });
        }

        return CompletionResult.Empty;
    }

    private StatValuesEuiMessage GetCargo()
    {
        // Okay so there's no easy way to do this with how pricing works
        // So we'll just get the first value for each prototype ID which is probably good enough for the majority.

        var values = new List<string[]>();
        var priceSystem = _entManager.System<PricingSystem>();
        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var prices = new HashSet<string>(256);
        var ents = _entManager.GetEntities().ToArray();

        foreach (var entity in ents)
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

            values.Add(new[]
            {
                id,
                $"{price:0}",
            });
        }

        var state = new StatValuesEuiMessage()
        {
            Title = Loc.GetString("stat-cargo-values"),
            Headers = new List<string>()
            {
                Loc.GetString("stat-cargo-id"),
                Loc.GetString("stat-cargo-price"),
            },
            Values = values,
        };

        return state;
    }

    private StatValuesEuiMessage GetItem()
    {
        var values = new List<string[]>();
        var itemSystem = _entManager.System<ItemSystem>();
        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var itemQuery = _entManager.GetEntityQuery<ItemComponent>();
        var items = new HashSet<string>(1024);
        var ents = _entManager.GetEntities().ToArray();

        foreach (var entity in ents)
        {
            if (!metaQuery.TryGetComponent(entity, out var meta))
                continue;

            var id = meta.EntityPrototype?.ID;

            // We'll add it even if we don't have it so we don't have to raise the event again because this is probably faster.
            if (id == null || !items.Add(id))
                continue;

            if (!itemQuery.TryGetComponent(entity, out var itemComp))
                continue;

            values.Add(new[]
            {
                id,
                $"{itemSystem.GetItemSizeLocale(itemComp.Size)}",
            });
        }

        var state = new StatValuesEuiMessage
        {
            Title = Loc.GetString("stat-item-values"),
            Headers = new List<string>
            {
                Loc.GetString("stat-item-id"),
                Loc.GetString("stat-item-price"),
            },
            Values = values,
        };

        return state;
    }

    private StatValuesEuiMessage GetMelee()
    {
        var values = new List<string[]>();
        var meleeName = _factory.GetComponentName(typeof(MeleeWeaponComponent));

        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract ||
                !proto.Components.TryGetValue(meleeName,
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
                (comp.Damage.GetTotal() * comp.AttackRate).ToString(),
                comp.AttackRate.ToString(CultureInfo.CurrentCulture),
                comp.Damage.GetTotal().ToString(),
                comp.Range.ToString(CultureInfo.CurrentCulture),
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

    private StatValuesEuiMessage GetLatheMessage()
    {
        var values = new List<string[]>();
        var priceSystem = _entManager.System<PricingSystem>();

        foreach (var proto in _proto.EnumeratePrototypes<LatheRecipePrototype>())
        {
            var cost = 0.0;

            foreach (var (material, count) in proto.RequiredMaterials)
            {
                var materialPrice = _proto.Index<MaterialPrototype>(material).Price;
                cost += materialPrice * count;
            }

            var sell = priceSystem.GetEstimatedPrice(_proto.Index<EntityPrototype>(proto.Result));

            values.Add(new[]
            {
                proto.ID,
                $"{cost:0}",
                $"{sell:0}",
            });
        }

        var state = new StatValuesEuiMessage()
        {
            Title = Loc.GetString("stat-lathe-values"),
            Headers = new List<string>()
            {
                Loc.GetString("stat-lathe-id"),
                Loc.GetString("stat-lathe-cost"),
                Loc.GetString("stat-lathe-sell"),
            },
            Values = values,
        };

        return state;
    }
}
