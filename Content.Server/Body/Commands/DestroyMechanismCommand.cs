using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems.Part;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    sealed class DestroyMechanismCommand : IConsoleCommand
    {
        public string Command => "destroymechanism";
        public string Description => "Destroys a mechanism from your entity";
        public string Help => $"Usage: {Command} <mechanism>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Only a player can run this command.");
                return;
            }

            if (args.Length == 0)
            {
                shell.WriteLine(Help);
                return;
            }

            if (player.AttachedEntity is not {} attached)
            {
                shell.WriteLine("You have no entity.");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(attached, out SharedBodyComponent? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            var mechanismName = string.Join(" ", args).ToLowerInvariant();

            var bodySystem = EntitySystem.Get<BodySystem>();
            var bodyPartSys = EntitySystem.Get<SharedBodyPartSystem>();

            foreach (var part in bodySystem.GetAllParts(attached, body))
            foreach (var mechanism in bodyPartSys.GetAllMechanisms(part.Owner, part))
            {
                if (mechanism.Name.ToLowerInvariant() == mechanismName)
                {
                    bodyPartSys.DeleteMechanism(attached, mechanism, part);
                    shell.WriteLine($"Mechanism with name {mechanismName} has been destroyed.");
                    return;
                }
            }

            shell.WriteLine($"No mechanism was found with name {mechanismName}.");
        }
    }
}
