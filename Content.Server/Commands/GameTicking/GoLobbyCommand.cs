#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Content.Shared;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server)]
    public class GoLobbyCommand : IClientCommand
    {
        public string Command => "golobby";
        public string Description => "Enables the lobby and restarts the round.";
        public string Help => $"Usage: {Command} / {Command} <preset>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            Type? preset = null;
            var presetName = string.Join(" ", args);

            var ticker = IoCManager.Resolve<IGameTicker>();

            if (args.Length > 0)
            {
                if (!ticker.TryGetPreset(presetName, out preset))
                {
                    shell.SendText(player, $"No preset found with name {presetName}");
                    return;
                }
            }

            var config = IoCManager.Resolve<IConfigurationManager>();
            config.SetCVar(CCVars.GameLobbyEnabled, true);

            ticker.RestartRound();

            if (preset != null)
            {
                ticker.SetStartPreset(preset);
            }

            shell.SendText(player, $"Enabling the lobby and restarting the round.{(preset == null ? "" : $"\nPreset set to {presetName}")}");
        }
    }
}
