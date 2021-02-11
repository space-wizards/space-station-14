#nullable enable
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Commands.Body
{
    [AdminCommand(AdminFlags.Fun)]
    class RemoveHandCommand : IConsoleCommand
    {
        public string Command => "removehand";
        public string Description => "Removes a hand from your entity.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
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

            if (!player.AttachedEntity.TryGetComponent(out IBody? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            var (_, hand) = body.Parts.FirstOrDefault(x => x.Value.PartType == BodyPartType.Hand);
            if (hand == null)
            {
                shell.WriteLine("You have no hands.");
            }
            else
            {
                body.RemovePart(hand);
            }
        }
    }
}
