using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class StripAllCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override string Command => "stripall";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetUidNet) || !EntityManager.TryGetEntity(targetUidNet, out var targetEntity))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityManager.TryGetComponent<InventoryComponent>(targetEntity, out var inventory))
        {
            shell.WriteLine(Loc.GetString("shell-entity-target-lacks-component", ("componentName", nameof(InventoryComponent))));
            return;
        }

        var slots = _inventorySystem.GetSlotEnumerator((targetEntity.Value, inventory));
        while (slots.NextItem(out _, out var slot))
        {
            _inventorySystem.TryUnequip(targetEntity.Value, targetEntity.Value, slot.Name, true, true, inventory: inventory);
        }

        if (EntityManager.TryGetComponent<HandsComponent>(targetEntity, out var hands))
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<InventoryComponent>(args[0]),
                Loc.GetString("cmd-stripall-player-completion"));
        }

        return CompletionResult.Empty;
    }
}

