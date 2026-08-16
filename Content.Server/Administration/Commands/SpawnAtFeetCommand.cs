using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Spawn)]
public sealed partial class spawnnexttoCommand : LocalizedEntityCommands
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override string Command => "spawnnextto";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { Valid: true } player)
        {
            shell.WriteError(Loc.GetString("cmd-spawnnextto-error-no-player"));
            return;
        }

        if (args.Length is < 1 or > 3)
        {
            shell.WriteLine(Help);
            return;
        }

        var prototype = args[0];
        if (!_prototype.HasIndex<EntityPrototype>(prototype))
        {
            shell.WriteError(Loc.GetString("cmd-spawnnextto-error-no-prototype", ("prototype", prototype)));
            return;
        }

        var count = 1000;
        if (args.Length > 1 && (!int.TryParse(args[1], out count) || count < 1))
        {
            shell.WriteError(Loc.GetString("cmd-spawnnextto-error-count"));
            return;
        }

        for (var i = 0; i < count; i++)
        {
            EntityManager.SpawnNextToOrDrop(prototype, player);
        }

        shell.WriteLine(Loc.GetString("cmd-spawnnextto-success", ("count", count), ("prototype", prototype)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIdsLimited<EntityPrototype>(args[0], _prototype),
                Loc.GetString("cmd-spawnnextto-hint-prototype")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-spawnnextto-hint-count")),
            _ => CompletionResult.Empty,
        };
    }
}
