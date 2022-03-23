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
    public string Command => "playglobalsound";
    public string Description => "Plays a global sound for a specific player or for every connected player if no players are specified.";
    public string Help => $"playglobalsound <path> [user 1] ... [user n]\n{Description}";
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
                filter = Filter.Broadcast();
                break;

            // One or more users specified.
            default:
                var playerManager = IoCManager.Resolve<IPlayerManager>();
                filter = Filter.Empty();

                // Skip the first argument, which is the sound path.
                for (var i = 1; i < args.Length; i++)
                {
                    var username = args[i];

                    if (!playerManager.TryGetSessionByUsername(username, out var session))
                    {
                        shell.WriteError($"Player \"{username}\" not found.");
                        continue;
                    }

                    filter.AddPlayer(session);
                }
                break;
        }

        SoundSystem.Play(filter, args[0], AudioParams.Default);
    }
}
