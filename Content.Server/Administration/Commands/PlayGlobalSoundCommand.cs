using System.IO;
using System.Linq;
using Content.Server.Audio;
using Content.Shared.Administration;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

// This is for debugging nothing more.
[AdminCommand(AdminFlags.Debug)]
public sealed class PlayGlobalAudioCommand : IConsoleCommand
{
    public string Command => "playaudio";
    public string Description => "Plays audio globally for debugging";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        var resourceManager = IoCManager.Resolve<IResourceManager>();
        var audioSystem = entManager.System<AudioSystem>();
        var fileName = args[0];

        shell.WriteLine($"Checking {fileName} global audio");

        var audioLength = audioSystem.GetAudioLength(fileName);

        shell.WriteLine($"Cached audio length is: {audioLength}");

        // Copied code to get the actual length determination
        // Check shipped metadata from packaging.
        if (protoManager.TryIndex(fileName, out AudioMetadataPrototype? metadata))
        {
            shell.WriteLine($"Used prototype, length is: {metadata.Length}");
        }
        else if (!resourceManager.TryContentFileRead(fileName, out var stream))
        {
            throw new FileNotFoundException($"Unable to find metadata for audio file {fileName}");
        }
        else
        {
            shell.WriteLine("Looks like audio stream used and cached.");
        }

        var broadcastFilter = Filter.Broadcast();

        shell.WriteLine($"Playing filter to {broadcastFilter.Count} players");

        audioSystem.PlayGlobal(fileName, broadcastFilter, true);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayGlobalSoundCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "playglobalsound";
    public string Description => Loc.GetString("play-global-sound-command-description");
    public string Help => Loc.GetString("play-global-sound-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;
        var audio = AudioParams.Default;

        bool replay = true;

        switch (args.Length)
        {
            // No arguments, show command help.
            case 0:
                shell.WriteLine(Loc.GetString("play-global-sound-command-help"));
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
                    shell.WriteError(Loc.GetString("play-global-sound-command-volume-parse", ("volume", args[1])));
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
                            shell.WriteError(Loc.GetString("play-global-sound-command-player-not-found", ("username", username)));
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
            var hint = Loc.GetString("play-global-sound-command-arg-path");

            var options = CompletionHelper.ContentFilePath(args[0], _res);

            return CompletionResult.FromHintOptions(options, hint);
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("play-global-sound-command-arg-volume"));

        if (args.Length > 2)
        {
            var options = _playerManager.Sessions.Select<ICommonSession, string>(c => c.Name);
            return CompletionResult.FromHintOptions(
                options,
                Loc.GetString("play-global-sound-command-arg-usern", ("user", args.Length - 2)));
        }

        return CompletionResult.Empty;
    }
}
