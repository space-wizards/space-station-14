using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Administration.Commands;

/// <summary>
///     Command that allows admins to play global sounds.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class PlayGlobalSound : IConsoleCommand
{
    [Dependency] private IPlayerManager _playerManager = default!;


    public string Command => "playglobalsound";
    public string Description => Loc.GetString("play-global-sound-command-description");
    public string Help => Loc.GetString("play-global-sound-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;

        switch (args.Length)
        {
            // No arguments, show command help.
            case 0:
                shell.WriteLine(Help);
                return;

            // No users, play sound for everyone.
            case 1:
                // Filter.Broadcast does resolves IPlayerManager, so use this instead.
                filter = Filter.Empty().AddAllPlayers(_playerManager);
                break;

            // One or more users specified.
            default:
                filter = Filter.Empty();

                // Skip the first argument, which is the sound path.
                for (var i = 1; i < args.Length; i++)
                {
                    var username = args[i];

                    if (!_playerManager.TryGetSessionByUsername(username, out var session))
                    {
                        shell.WriteError(Loc.GetString("play-global-sound-command-player-not-found", ("username", username)));
                        continue;
                    }

                    filter.AddPlayer(session);
                }
                break;
        }

        SoundSystem.Play(filter, args[0], AudioParams.Default);
    }
}
