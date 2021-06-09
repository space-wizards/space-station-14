#nullable enable
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    class AddHandCommand : IConsoleCommand
    {
        public const string DefaultHandPrototype = "LeftHandHuman";

        public string Command => "addhand";
        public string Description => "Adds a hand to your entity.";
        public string Help => $"Usage: {Command} <entityUid> <handPrototypeId> / {Command} <entityUid> / {Command} <handPrototypeId> / {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length > 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            IEntity entity;
            IEntity hand;

            switch (args.Length)
            {
                case 0:
                {
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

                    entity = player.AttachedEntity;
                    hand = entityManager.SpawnEntity(DefaultHandPrototype, entity.Transform.Coordinates);
                    break;
                }
                case 1:
                {
                    if (EntityUid.TryParse(args[0], out var uid))
                    {
                        if (!entityManager.TryGetEntity(uid, out var parsedEntity))
                        {
                            shell.WriteLine($"No entity found with uid {uid}");
                            return;
                        }

                        entity = parsedEntity;
                        hand = entityManager.SpawnEntity(DefaultHandPrototype, entity.Transform.Coordinates);
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

                        entity = player.AttachedEntity;
                        hand = entityManager.SpawnEntity(args[0], entity.Transform.Coordinates);
                    }

                    break;
                }
                case 2:
                {
                    if (!EntityUid.TryParse(args[0], out var uid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    if (!entityManager.TryGetEntity(uid, out var parsedEntity))
                    {
                        shell.WriteLine($"No entity exists with uid {uid}.");
                        return;
                    }

                    entity = parsedEntity;

                    if (!prototypeManager.HasIndex<EntityPrototype>(args[1]))
                    {
                        shell.WriteLine($"No hand entity exists with id {args[1]}.");
                        return;
                    }

                    hand = entityManager.SpawnEntity(args[1], entity.Transform.Coordinates);

                    break;
                }
                default:
                {
                    shell.WriteLine(Help);
                    return;
                }
            }

            if (!entity.TryGetComponent(out IBody? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.WriteLine(text);
                return;
            }

            if (!hand.TryGetComponent(out IBodyPart? part))
            {
                shell.WriteLine($"Hand entity {hand} does not have a {nameof(IBodyPart)} component.");
                return;
            }

            var slot = part.GetHashCode().ToString();
            body.SetPart(slot, part);

            shell.WriteLine($"Added hand to entity {entity.Name}");
        }
    }
}
