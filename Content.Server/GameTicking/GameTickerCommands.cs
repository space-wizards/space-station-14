using System;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.GameTicking
{

    class StartRoundCommand : IClientCommand
    {
        public string Command => "startround";
        public string Description => "Ends PreRoundLobby state and starts the round.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            ticker.StartRound();
        }
    }

    class EndRoundCommand : IClientCommand
    {
        public string Command => "endround";
        public string Description => "Ends the round and moves the server to PostRound.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.InRound)
            {
                shell.SendText(player, "This can only be executed while the game is in a round.");
                return;
            }

            ticker.EndRound();
        }
    }

    class NewRoundCommand : IClientCommand
    {
        public string Command => "restartround";
        public string Description => "Moves the server from PostRound to a new PreRoundLobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.RestartRound();
        }
    }

    class RespawnCommand : IClientCommand
    {
        public string Command => "respawn";
        public string Description => "Respawns a player, kicking them back to the lobby.";
        public string Help => "respawn [player]";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length > 1)
            {
                shell.SendText(player, "Must provide <= 1 argument.");
                return;
            }

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var ticker = IoCManager.Resolve<IGameTicker>();

            NetSessionId sessionId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.SendText((IPlayerSession)null, "If not a player, an argument must be given.");
                    return;
                }

                sessionId = player.SessionId;
            }
            else
            {
                sessionId = new NetSessionId(args[0]);
            }

            if (!playerMgr.TryGetSessionById(sessionId, out var targetPlayer))
            {
                if (!playerMgr.TryGetPlayerData(sessionId, out var data))
                {
                    shell.SendText(player, "Unknown player");
                    return;
                }

                data.ContentData().WipeMind();
                shell.SendText(player,
                    "Player is not currently online, but they will respawn if they come back online");
                return;
            }

            ticker.Respawn(targetPlayer);
        }
    }

    class ObserveCommand : IClientCommand
    {
        public string Command => "observe";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.MakeObserve(player);
        }
    }

    class JoinGameCommand : IClientCommand
    {
        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "Round has not started.");
                return;
            }

            ticker.MakeJoinGame(player);
        }
    }

    class ToggleReadyCommand : IClientCommand
    {
        public string Command => "toggleready";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.ToggleReady(player, bool.Parse(args[0]));
        }
    }

    class SetGamePresetCommand : IClientCommand
    {
        public string Command => "setgamepreset";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Need exactly one argument.");
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();

            Type presetType;
            switch (args[0])
            {
                case "DeathMatch":
                    presetType = typeof(PresetDeathMatch);
                    break;
                case "Sandbox":
                    presetType = typeof(PresetSandbox);
                    break;
                default:
                    shell.SendText(player, "That is not a valid game preset!");
                    return;
            }

            ticker.SetStartPreset(presetType);
        }
    }
}
