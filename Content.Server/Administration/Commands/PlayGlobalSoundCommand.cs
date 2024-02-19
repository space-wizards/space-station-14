using System.Linq;
using Content.Server.Audio;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Utility;
using static Content.Server.Audio.ServerGlobalSoundSystem;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayGlobalSoundCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

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
            var options2 = new List<CompletionOption>();
            try
            {
                var path = new ResPath(args[0]);
                foreach (var file in _res.ContentFindFiles(path))
                {
                    if (file.Extension != "yml")
                        continue;

                    var yamlData = _res.ContentFileReadYaml(file);

                    if (yamlData.Documents.Count == 0)
                        continue;

                    foreach (var item in yamlData.Documents[0].AllNodes)
                    {
                        var sequence = (SequenceDataNode) item.ToDataNode();
                        foreach (var entry in sequence.Sequence)
                        {
                            if (entry is not MappingDataNode map)
                                continue;
                            if (!map.TryGet("files", out var value))
                                continue;

                            foreach (var item1 in (SequenceDataNode) value)
                            {
                                options2.Add(new CompletionOption(file.WithName(item1.ToString()).CanonPath));
                            }
                        }
                    }
                }
            }
            catch (Exception) {}
            var options = CompletionHelper.ContentFilePath(args[0], _res);

#if FULL_RELEASE
            return CompletionResult.FromHintOptions(options.Concat(options2), hint);
#else
            // We don't need to concat lists in debug version since it works here
            return CompletionResult.FromHintOptions(options, hint);
#endif

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
