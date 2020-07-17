using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Health.BodySystem;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.BodySystem;
using Content.Shared.Jobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking
{
    class DelayStartCommand : IClientCommand
    {
        public string Command => "delaystart";
        public string Description => "Delays the round start.";
        public string Help => $"Usage: {Command} <seconds>\nPauses/Resumes the countdown if no argument is provided.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            if (args.Length == 0)
            {
                var paused = ticker.TogglePause();
                shell.SendText(player, paused ? "Paused the countdown." : "Resumed the countdown.");
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, "Need zero or one arguments.");
                return;
            }

            if (!uint.TryParse(args[0], out var seconds) || seconds == 0)
            {
                shell.SendText(player, $"{args[0]} isn't a valid amount of seconds.");
                return;
            }

            var time = TimeSpan.FromSeconds(seconds);
            if (!ticker.DelayStart(time))
            {
                shell.SendText(player, "An unknown error has occurred.");
            }
        }
    }

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
#pragma warning disable 649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649
        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var output = string.Join(".", args);
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
            else if(ticker.RunLevel == GameRunLevel.InRound)
            {
                string ID = args[0];
                var positions = ticker.GetAvailablePositions();

                if(positions.GetValueOrDefault(ID, 0) == 0) //n < 0 is treated as infinite
                {
                    var jobPrototype = _prototypeManager.Index<JobPrototype>(ID);
                    shell.SendText(player, $"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, args[0].ToString());
                return;
            }

            ticker.MakeJoinGame(player, null);
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

            ticker.SetStartPreset(args[0]);
        }
    }

    class ForcePresetCommand : IClientCommand
    {
        public string Command => "forcepreset";
        public string Description => "Forces a specific game preset to start for the current lobby.";
        public string Help => $"Usage: {Command} <preset>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, "Need exactly one argument.");
                return;
            }

            var name = args[0];
            if (!ticker.TryGetPreset(name, out var type))
            {
                shell.SendText(player, $"No preset exists with name {name}.");
                return;
            }

            ticker.SetStartPreset(type, true);
            shell.SendText(player, $"Forced the game to start with preset {name}.");
        }
    }

    class AddHandCommand : IClientCommand
    {
        public string Command => "addhand";
        public string Description => "Forces a specific game preset to start for the current lobby.";
        public string Help => $"Usage: {Command} <preset>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var manager = player.AttachedEntity.GetComponent<BodyManagerComponent>();
            var hand = manager.PartDictionary.First(x => x.Key == string.Join(" ", args));
            manager.InstallBodyPart(hand.Value, hand.Key + new Random());
        }
    }

    class RemoveHandCommand : IClientCommand
    {
        public string Command => "removehand";
        public string Description => "Forces a specific game preset to start for the current lobby.";
        public string Help => $"Usage: {Command} <preset>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var manager = player.AttachedEntity.GetComponent<BodyManagerComponent>();
            var hand = manager.PartDictionary.First(x => x.Value.PartType == BodyPartType.Hand);
            manager.DisconnectBodyPart(hand.Value, true);
        }
    }
}
