using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.ErtCall;

[AdminCommand(AdminFlags.Admin)]
public sealed class CallErt : LocalizedCommands
{
    public string Description => Loc.GetString("callertcommand-desc");
    public string Help => Loc.GetString("callertcommand-help");

    public override string Command => "callert";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = IoCManager.Resolve<IPrototypeManager>()
                .EnumeratePrototypes<ErtCallPresetPrototype>()
                .Select(p => new CompletionOption(p.ID, p.Desc));

            return CompletionResult.FromHintOptions(options, Loc.GetString("callertcommand-id-preset"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args0"));
            SoundSystem.Play("/Audio/Corvax/Adminbuse/noert.ogg", Filter.Broadcast(), AudioParams.Default.WithVolume(-2f));
            return;
        }
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args1"));
            return;
        }
        var ertSpawnSystem = IoCManager.Resolve<IEntityManager>().System<CallErtSystem>();
        var protoId = args[0];
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypeManager.TryIndex<ErtCallPresetPrototype>(protoId, out var proto))
        {
            shell.WriteError(Loc.GetString("callertcommand-error-prest-not-found", ("protoid", protoId)));
            return;
        }
        if (ertSpawnSystem.SpawnErt(proto))
        {
            SoundSystem.Play("/Audio/Corvax/Adminbuse/yesert.ogg", Filter.Broadcast(), AudioParams.Default.WithVolume(-5f));
            shell.WriteLine(Loc.GetString("callertcommand-preset-loaded", ("protoid", protoId)));
            return;
        }
        else
        {
            shell.WriteError(Loc.GetString("callertcommand-error-when-load-grid"));
            return;
        }
    }
}

