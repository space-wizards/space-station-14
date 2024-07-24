using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class EndRoundCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "endround";
        public string Description => "Ends the round and moves the server to PostRound.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ticker = _e.System<GameTicker>();

            if (ticker.RunLevel != GameRunLevel.InRound)
            {
                shell.WriteLine("This can only be executed while the game is in a round.");
                return;
            }

            ticker.EndRound();
        }
    }
}
