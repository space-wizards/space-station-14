using System.Linq;
using Content.Server.Administration;
using Content.Server.Store.Systems;
using Content.Shared.Administration;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Console;

namespace Content.Server.Store.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class AddCurrency : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityMan = default!;

    public string Command => "addcurrency";
    public string Description => "Adds currency to the specified store";
    public string Help => "addcurrency <uid> <currency prototype> <amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError("Argument length must be 3");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !_entityMan.TryGetEntity(uidNet, out var uid) || !float.TryParse(args[2], out var id))
        {
            return;
        }

        if (!_entityMan.TryGetComponent<StoreComponent>(uid, out var store))
            return;

        var currency = new Dictionary<string, FixedPoint2>
        {
            { args[1], id }
        };

        var storeSys = _entityMan.System<StoreSystem>();
        storeSys.TryAddCurrency(currency, (uid.Value, store));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var query = _entityMan.EntityQueryEnumerator<StoreComponent>();
            var allStores = new List<string>();
            while (query.MoveNext(out var storeuid, out _))
            {
                allStores.Add(storeuid.ToString());
            }
            return CompletionResult.FromHintOptions(allStores, "<uid>");
        }

        if (args.Length == 2 && NetEntity.TryParse(args[0], out var uidNet) && _entityMan.TryGetEntity(uidNet, out var uid))
        {
            if (_entityMan.TryGetComponent<StoreComponent>(uid, out var store))
                return CompletionResult.FromHintOptions(store.CurrencyWhitelist.Select(p => p.ToString()), "<currency prototype>");
        }

        return CompletionResult.Empty;
    }
}
