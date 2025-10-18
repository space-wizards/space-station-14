using Content.Server.Administration;
using Content.Server.Database.Migrations.Postgres;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Server.Nutrition;

[AdminCommand(AdminFlags.Debug)]
public sealed class SetNutrit : LocalizedEntityCommands
{
    public override string Command => "setnutrit";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("cmd-nutrition-error-player"));
            return;
        }

        if (player.AttachedEntity is not { Valid: true } playerEntity)
        {
            shell.WriteError(Loc.GetString("cmd-nutrition-error-entity"));
            return;
        }

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)
            ));
            return;
        }

        var systemString = args[0];
        switch (systemString)
        {
            case "hunger":
            {
                if (!EntityManager.TryGetComponent(playerEntity, out HungerComponent? hunger))
                {
                    shell.WriteError(Loc.GetString("cmd-nutrition-error-component", ("comp", nameof(HungerComponent))));
                    return;
                }

                if (!Enum.TryParse(args[1], out HungerThreshold hungerThreshold))
                {
                    shell.WriteError(Loc.GetString("cmd-setnutrit-error-invalid-threshold",
                        ("thresholdType", nameof(HungerThreshold)),
                        ("thresholdString", args[1])
                    ));
                    return;
                }

                var hungerValue = hunger.Thresholds[hungerThreshold];
                EntityManager.System<HungerSystem>().SetHunger(playerEntity, hungerValue, hunger);
                return;
            }
            case "thirst":
            {
                if (!EntityManager.TryGetComponent(playerEntity, out ThirstComponent? thirst))
                {
                    shell.WriteError(Loc.GetString("cmd-nutrition-error-component", ("comp", nameof(ThirstComponent))));
                    return;
                }

                if (!Enum.TryParse(args[1], out ThirstThreshold thirstThreshold))
                {
                    shell.WriteError(Loc.GetString("cmd-setnutrit-error-invalid-threshold",
                         ("thresholdType", nameof(ThirstThreshold)),
                         ("thresholdString", args[1])
                     ));
                    return;
                }

                var thirstValue = thirst.ThirstThresholds[thirstThreshold];
                EntityManager.System<ThirstSystem>().SetThirst(playerEntity, thirst, thirstValue);
                return;
            }
            default:
            {
                shell.WriteError($"invalid nutrition system ${systemString}");
                return;
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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
