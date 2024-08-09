using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class RemoveHandCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public string Command => "removehand";
        public string Description => "Removes a hand from your entity.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("Only a player can run this command.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.WriteLine("You have no entity.");
                return;
            }

            if (!_entManager.TryGetComponent(player.AttachedEntity, out BodyComponent? body))
            {
                var text = $"You have no body{(_random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            var bodySystem = _entManager.System<BodySystem>();
            var hand = bodySystem.GetBodyChildrenOfType(player.AttachedEntity.Value, BodyPartType.Hand, body).FirstOrDefault();

            if (hand == default)
            {
                shell.WriteLine("You have no hands.");
            }
            else
            {
                _entManager.System<SharedTransformSystem>().AttachToGridOrMap(hand.Id);
            }
        }
    }
}
