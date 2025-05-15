using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class StripAllCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override string Command => "stripall";
    public override string Description => Loc.GetString("cmd-stripall-desc");
    public override string Help => Loc.GetString("cmd-stripall-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetUidNet) || !_entManager.TryGetEntity(targetUidNet, out var targetEntity))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.TryGetComponent<InventoryComponent>(targetEntity, out var inventory))
        {
            shell.WriteLine(Loc.GetString("shell-entity-target-lacks-component", ("componentName", nameof(InventoryComponent))));
            return;
        }

        var slots = _inventorySystem.GetSlotEnumerator((targetEntity.Value, inventory));
        while (slots.NextItem(out _, out var slot))
        {
            _inventorySystem.TryUnequip(targetEntity.Value, targetEntity.Value, slot.Name, true, true, inventory: inventory);
        }

        if (_entManager.TryGetComponent<HandsComponent>(targetEntity, out var hands))
        {
            foreach (var hand in _handsSystem.EnumerateHands(targetEntity.Value, hands))
            {
                _handsSystem.TryDrop(targetEntity.Value,
                    hand,
                    checkActionBlocker: false,
                    doDropInteraction: false,
                    handsComp: hands);
            }
        }
    }

    private IEnumerable<string> GetEntitiesWithInventory()
    {
        List<string> points = new(_entManager.Count<InventoryComponent>());
        var query = _entManager.AllEntityQueryEnumerator<InventoryComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            points.Add(uid.ToString());
        }

        points.Sort();
        return points;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = GetEntitiesWithInventory();

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-stripall-player-completion"));
        }

        return CompletionResult.Empty;
    }
}

