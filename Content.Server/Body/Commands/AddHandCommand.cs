using Content.Server.Administration;
using Content.Server.Hands.Systems;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    sealed class AddHandCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        private static int _handIdAccumulator;

        public string Command => "addhand";
        public string Description => "Adds a hand to your entity.";
        public string Help => $"Usage: {Command} <entityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            EntityUid entity;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.WriteLine("Only a player can run this command without arguments.");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine("You don't have an entity to add a hand to.");
                        return;
                    }

                    entity = player.AttachedEntity.Value;
                    break;
                case 1:
                    {
                        if (NetEntity.TryParse(args[0], out var uidNet) && _entManager.TryGetEntity(uidNet, out var uid))
                        {
                            if (!_entManager.EntityExists(uid))
                            {
                                shell.WriteLine($"No entity found with uid {uid}");
                                return;
                            }

                            entity = uid.Value;
                        }
                        else
                        {
                            if (player == null)
                            {
                                shell.WriteLine("You must specify an entity to add a hand to when using this command from the server terminal.");
                                return;
                            }

                            if (player.AttachedEntity == null)
                            {
                                shell.WriteLine("You don't have an entity to add a hand to.");
                                return;
                            }

                            entity = player.AttachedEntity.Value;
                        }

                        break;
                    }
                default:
                    shell.WriteLine(Help);
                    return;
            }

            _entManager.System<HandsSystem>().AddHand(entity, $"cmd-{_handIdAccumulator++}", HandLocation.Middle);

            shell.WriteLine($"Added hand to entity {_entManager.GetComponent<MetaDataComponent>(entity).EntityName}");
        }
    }
}
