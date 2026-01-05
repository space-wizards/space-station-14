using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayLocalSoundCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public override string Command => "playlocalsound";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var filter = Filter.Empty();
        var audio = AudioParams.Default;

        var replay = true;
        var followRunner = false;

        var playerEntity = shell.Player?.AttachedEntity;
        if (playerEntity == null)
        {
            shell.WriteLine(Loc.GetString("cmd-playlocalsound-no-entity"));
            return;
        }

        if (args.Length == 0) // No arguments, show command help.
        {
            shell.WriteLine(Loc.GetString("cmd-playlocalsound-help"));
            return;
        }

        // No players specified, so set filter here
        if (args.Length <= 3)
        {
            // Filter.Broadcast does resolves IPlayerManager, so use this instead.
            filter = Filter.Empty().AddAllPlayers(_playerManager);
        }

        // At least <path> [volume] specified
        if (args.Length >= 2)
        {
            if (int.TryParse(args[1], out var volume))
            {
                audio = audio.WithVolume(volume);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-playlocalsound-volume-parse", ("volume", args[1])));
                return;
            }
        }

        // <path> [volume] [follow] specified
        if (args.Length >= 3 && !bool.TryParse(args[2], out followRunner))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        // One or more users specified.
        if (args.Length >= 4)
        {
            replay = false;
            // <path> [volume] [follow] >[players...]
            for (var i = 3; i < args.Length; i++)
            {
                var username = args[i];

                if (!_playerManager.TryGetSessionByUsername(username, out var session))
                {
                    shell.WriteError(Loc.GetString("cmd-playlocalsound-player-not-found",
                        ("username", username)));
                    continue; // Still use remaining names
                }

                filter.AddPlayer(session);
            }
        }

        audio = audio.AddVolume(-8); // Matching /playglobalsound
        var sound = new SoundPathSpecifier(args[0], audio);

        if (followRunner)
        {
            // Note: if playerEntity isn't in a client's PVS (the runner could be a ghost), they will
            // never hear this. This is not true if followRunner is false. This is not totally ideal.
            _audioSystem.PlayEntity(sound, filter, playerEntity.Value, replay);
        }
        else
        {
            // Whatever the runner's parent is is /probably/ what we want to be stationary to.
            var coords = EntityManager.GetComponent<TransformComponent>(playerEntity.Value).Coordinates;
            _audioSystem.PlayStatic(sound, filter, coords, replay);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.AudioFilePath(args[0], _protoManager, _res),
                Loc.GetString("cmd-playlocalsound-arg-path")),

            2 => CompletionResult.FromHint(Loc.GetString("cmd-playlocalsound-arg-volume")),

            3 => CompletionResult.FromHint(Loc.GetString("cmd-playlocalsound-arg-follow")),

            >= 4 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-playlocalsound-arg-usern", ("user", args.Length - 3))),

            _ => CompletionResult.Empty,
        };
    }
}
