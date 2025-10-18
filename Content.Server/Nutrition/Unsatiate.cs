using System.IO.Compression;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition;

/// <summary>
/// This command sets the specified <see cref="Satiation">Satiation(s)</see> to <see cref="SatiationThreshold.Desperate"/>.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class Unsatiate : LocalizedEntityCommands
{
    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override string Command => "unsatiate";

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

        if (!EntityManager.TryGetComponent(playerEntity, out SatiationComponent? satiation))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component",
                ("uid", playerEntity.Id),
                ("componentName", nameof(SatiationComponent))));
            return;
        }

        if (args.Length == 0)
        {
            ExecuteWithoutArgs((playerEntity, satiation));
        }
        else
        {
            ExecuteWithArgs(shell, (playerEntity, satiation), args);
        }
    }

    private void ExecuteWithoutArgs(Entity<SatiationComponent> entity)
    {
        foreach (var satiation in entity.Comp.Satiations.Keys)
        {
            _satiation.SetValue(entity, satiation, SatiationThreshold.Desperate);
        }
    }

    private void ExecuteWithArgs(IConsoleShell shell, Entity<SatiationComponent> entity, string[] args)
    {
        // Check all of the types given before modifying any of them.
        foreach (var arg in args)
        {
            if (_satiation.GetTypeOrNull(arg) is { } satiationType && entity.Comp.Has(satiationType))
                continue;

            shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message",
                ("missing", Loc.GetString("cmd-nutrition-satiation-need", ("satiation", arg)))));
            return;
        }

        foreach (var arg in args)
        {
            _satiation.SetValue(entity, arg, SatiationThreshold.Desperate);
        }
    }

    // Always suggest any satiation types which aren't already in the arg list.
    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var absentSatiationTypeIds = _satiation.GetTypes().Select(type => type.ID).Where(type => !args.Contains(type));
        return CompletionResult.FromHintOptions(absentSatiationTypeIds, nameof(SatiationTypePrototype));
    }
}
