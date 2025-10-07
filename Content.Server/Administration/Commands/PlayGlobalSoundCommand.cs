using System.Linq;
using Content.Server.Audio;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayGlobalSoundCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public override string Command => "playglobalsound";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;
        var audio = AudioParams.Default;

        bool replay = true;

        switch (args.Length)
        {
            // No arguments, show command help.
            case 0:
                shell.WriteLine(Loc.GetString($"cmd-{Command}-help"));
                return;

            // No users, play sound for everyone.
            case 1:
                // Filter.Broadcast does resolves IPlayerManager, so use this instead.
                filter = Filter.Empty().AddAllPlayers(_playerManager);
                break;

            // One or more users specified.
            default:
                var volumeOffset = 0;

                // Try to specify a new volume to play it at.
                if (int.TryParse(args[1], out var volume))
                {
                    audio = audio.WithVolume(volume);
                    volumeOffset = 1;
                }
                else
                {
                    shell.WriteError(Loc.GetString($"cmd-{Command}-volume-parse", ("volume", args[1])));
                    return;
                }

                // No users specified so play for them all.
                if (args.Length == 2)
                {
                    filter = Filter.Empty().AddAllPlayers(_playerManager);
                }
                else
                {
                    replay = false;

                    filter = Filter.Empty();

                    // Skip the first argument, which is the sound path.
                    for (var i = 1 + volumeOffset; i < args.Length; i++)
                    {
                        var username = args[i];

                        if (!_playerManager.TryGetSessionByUsername(username, out var session))
                        {
                            shell.WriteError(Loc.GetString($"cmd-{Command}-player-not-found", ("username", username)));
                            continue;
                        }

                        filter.AddPlayer(session);
                    }
                }

                break;
        }

        audio = audio.AddVolume(-8);
        _entManager.System<ServerGlobalSoundSystem>().PlayAdminGlobal(filter, args[0], audio, replay);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var hint = Loc.GetString($"cmd-{Command}-arg-path");

            var options = CompletionHelper.AudioFilePath(args[0], _protoManager, _res);

            return CompletionResult.FromHintOptions(options, hint);
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-volume"));

        if (args.Length > 2)
        {
            var options = _playerManager.Sessions.Select<ICommonSession, string>(c => c.Name);
            return CompletionResult.FromHintOptions(
                options,
                Loc.GetString($"cmd-{Command}-arg-usern", ("user", args.Length - 2)));
        }

        return CompletionResult.Empty;
    }
}
