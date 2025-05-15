using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class StripAllCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "stripall";
        public string Description => "Strips an entity of all their inventory and hands.";

        public string Help =>
            "stripall <entity>\nStrips an entity of all their inventory and hands. " +
            "Autofill shows all entities with an inventory.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Expected a single argument.");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var entInt))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!_entManager.TryGetComponent<InventoryComponent>(entInt, out var inventory))
            {
                shell.WriteLine("Entity must have an inventory.");
                return;
            }

            var inventorySystem = _entManager.System<InventorySystem>();
            var handsSystem = _entManager.System<SharedHandsSystem>();

            var slots = inventorySystem.GetSlotEnumerator((entInt, inventory));
            while (slots.NextItem(out _, out var slot))
            {
                inventorySystem.TryUnequip(entInt, entInt, slot.Name, true, true, inventory: inventory);
            }

            if (_entManager.TryGetComponent<HandsComponent>(entInt, out var hands))
            {
                foreach (var hand in handsSystem.EnumerateHands(entInt, hands))
                {
                    handsSystem.TryDrop(entInt, hand, checkActionBlocker: false, doDropInteraction: false, handsComp: hands);
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

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = new[] { "?" }.Concat(GetEntitiesWithInventory());

                return CompletionResult.FromHintOptions(options, "<EntityUid>");
            }

            return CompletionResult.Empty;
        }
    }
}
