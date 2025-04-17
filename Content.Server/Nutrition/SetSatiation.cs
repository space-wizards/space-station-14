using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;
using System.Linq;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition;

// TODO maybe change this to "set satiation" or something.
[AdminCommand(AdminFlags.Debug)]
public sealed class SetSatiation : LocalizedEntityCommands
{
    public override string Command => "setsatiation";

    [Dependency] private readonly SatiationSystem _satiation = null!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity is not { Valid: true } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
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

        if (!EntityManager.TryGetComponent<SatiationComponent>(playerEntity, out var satiation))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component",
                ("uid", playerEntity.Id),
                ("componentName", nameof(SatiationComponent))));
            return;
        }

        ProtoId<SatiationTypePrototype> type = args[0];
        if (!satiation.Has(type))
        {
            var typeName = _satiation.GetTypeOrNull(type)?.Name is { } locId ? Loc.GetString(locId) : $"\"{type}\"";
            shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message",
                ("missing", Loc.GetString("cmd-nutrition-satiation-need", ("satiation", typeName)))));
            return;
        }

        if (!Enum.TryParse(args[1], out SatiationThreshold satiationThreshold))
        {
            // It's not technically a prototype, but it's close enough.
            shell.WriteError(Loc.GetString("shell-argument-must-be-prototype",
                ("index", 1),
                ("prototypeName", nameof(SatiationThreshold))
            ));
            return;
        }

        _satiation.SetValue((playerEntity, satiation), type, satiationThreshold);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(_satiation.GetTypes().Select(type => type.ID),
                nameof(SatiationTypePrototype)),
            2 => CompletionResult.FromHintOptions(Enum.GetNames<SatiationThreshold>(), nameof(SatiationThreshold)),
            _ => CompletionResult.Empty
        };
    }
}
