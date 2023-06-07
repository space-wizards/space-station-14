using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.ErtCall;

[AdminCommand(AdminFlags.Admin)]
public sealed class CallErt : LocalizedCommands
{


    public override string Command => "callert";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = IoCManager.Resolve<IPrototypeManager>()
                .EnumeratePrototypes<ErtCallPresetPrototype>()
                .Select(p => new CompletionOption(p.ID));

            return CompletionResult.FromHintOptions(options, Loc.GetString("send-station-goal-command-arg-id"));//Переписать локализацию
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("Аргументов не может быть 0")); //Дописать локализацию. shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
            return;
        }
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("аргумент должен быть 1")); //Дописать локализацию. shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
            return;
        }
        var ertSpawnSystem = IoCManager.Resolve<IEntityManager>().System<CallErtSystem>();
        var protoId = args[0];
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<ErtCallPresetPrototype>(protoId, out var proto))
            {
                shell.WriteError($"No station goal found with ID {protoId}!");
                return;
            }
        if (ertSpawnSystem.SpawnErt(proto))
        {
            shell.WriteLine(Loc.GetString("пресет ОБР загружен на карту id"));
            return;
        }
        else
        {
            shell.WriteError("ошибка загрузки");
            return;
        }
    }
}

