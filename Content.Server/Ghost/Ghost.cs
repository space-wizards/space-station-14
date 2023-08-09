using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Ghost
{
    [AnyCommand]
    public sealed class Ghost : IConsoleCommand
    {
        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You have no session, you can't ghost.");
                return;
            }

            var mind = player.ContentData()?.Mind;
            if (mind == null)
            {
                shell.WriteLine("You have no Mind, you can't ghost.");
                return;
            }

            if (!EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, true, viaCommand:true))
            {
                shell.WriteLine("You can't ghost right now.");
            }
        }
    }
}
