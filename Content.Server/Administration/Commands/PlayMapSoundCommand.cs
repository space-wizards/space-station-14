using Content.Shared.Administration;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayMapSoundCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "playmapsound";
    public string Description => Loc.GetString("play-map-sound-command-description");
    public string Help => Loc.GetString("play-map-sound-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;
        var audio = AudioParams.Default;
        var player = shell.Player as ICommonSession;

        if (player?.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        filter = Filter.Empty().AddInMap(_entManager.System<TransformSystem>().GetMapId(player.AttachedEntity.Value));

        if (args.Length == 2)
        {
            if (int.TryParse(args[1], out var volume))
            {
                audio = audio.WithVolume(volume);
            }
            else
            {
                shell.WriteError(Loc.GetString("play-global-sound-command-volume-parse", ("volume", args[1])));
                return;
            }
        }

        audio = audio.AddVolume(-8);
        _entManager.System<AudioSystem>().PlayGlobal(args[0], filter, false, audio);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var hint = Loc.GetString("play-global-sound-command-arg-path");

            var options = CompletionHelper.AudioFilePath(args[0], _protoManager, _res);

            return CompletionResult.FromHintOptions(options, hint);
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("play-global-sound-command-arg-volume"));

        return CompletionResult.Empty;
    }
}
