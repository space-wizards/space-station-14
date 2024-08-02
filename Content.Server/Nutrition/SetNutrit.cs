using Content.Server.Administration;
using Content.Server.Database.Migrations.Postgres;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Server.Nutrition
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetNutrit : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "setnutrit";
        public string Description => "Modify your hunger or thirst";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command unless you are a player.");
                return;
            }

            if (player.AttachedEntity is not { Valid: true } playerEntity)
            {
                shell.WriteLine("You cannot use this command without an entity.");
                return;
            }

            if (args.Length != 2)
            {
                shell.WriteLine($"Wrong number of arguments! Expected 2 got {args.Length}");
                return;
            }

            var systemString = args[0];
            switch (systemString)
            {
                case "hunger":
                {
                    if (!_entities.TryGetComponent(playerEntity, out HungerComponent? hunger))
                    {
                        shell.WriteLine($"Your entity does not have a {nameof(HungerComponent)} component.");
                        return;
                    }

                    if (!Enum.TryParse(args[1], out HungerThreshold hungerThreshold))
                    {
                        shell.WriteError($"Invalid {nameof(HungerThreshold)} `{args[1]}`");
                        return;
                    }

                    var hungerValue = hunger.Thresholds[hungerThreshold];
                    _entities.System<HungerSystem>().SetHunger(playerEntity, hungerValue, hunger);
                    return;
                }
                case "thirst":
                {
                    if (!_entities.TryGetComponent(playerEntity, out ThirstComponent? thirst))
                    {
                        shell.WriteLine($"Your entity does not have a {nameof(ThirstComponent)} component.");
                        return;
                    }

                    if (!Enum.TryParse(args[1], out ThirstThreshold thirstThreshold))
                    {
                        shell.WriteError($"Invalid {nameof(ThirstThreshold)} `{args[1]}`");
                        return;
                    }

                    var thirstValue = thirst.ThirstThresholds[thirstThreshold];
                    _entities.System<ThirstSystem>().SetThirst(playerEntity, thirst, thirstValue);
                    return;
                }
                default:
                {
                    shell.WriteError($"invalid nutrition system ${systemString}");
                    return;
                }
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                {
                    string[] kinds = { "hunger", "thirst" };
                    return CompletionResult.FromHintOptions(kinds, "nutrition system");
                }
                case 2:
                {
                    return args[0] switch
                    {
                        "hunger" => CompletionResult.FromHintOptions(Enum.GetNames<HungerThreshold>(), nameof(HungerThreshold)),
                        "thirst" => CompletionResult.FromHintOptions(Enum.GetNames<ThirstThreshold>(), nameof(ThirstThreshold)),
                        _ => CompletionResult.Empty,
                    };
                }
                default:
                    return CompletionResult.Empty;
            }
        }
    }
}
