﻿using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class ForcePresetCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly GameTicker _ticker = default!;

        public override string Command => "forcepreset";

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-preround-lobby-only"));
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString($"shell-need-exactly-one-argument"));
                return;
            }

            var name = args[0];
            if (!_ticker.TryFindGamePreset(name, out var type))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-no-preset-found", ("preset", name)));
                return;
            }

            _ticker.SetGamePreset(type, true);
            shell.WriteLine(Loc.GetString($"cmd-{Command}-success", ("preset", name)));
            _ticker.UpdateInfoText();
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _prototypeManager
                    .EnumeratePrototypes<GamePresetPrototype>()
                    .OrderBy(p => p.ID)
                    .Select(p => p.ID);

                return CompletionResult.FromHintOptions(options, Loc.GetString($"cmd-{Command}-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
