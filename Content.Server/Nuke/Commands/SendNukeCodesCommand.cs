﻿using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SendNukeCodesCommand : IConsoleCommand
    {
        public string Command => "nukecodes";
        public string Description => "Send nuke codes to a station's communication consoles";
        public string Help => "nukecodes [station EntityUid]";

        [Dependency] private readonly IEntityManager _entityManager = default!;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entityManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            _entityManager.System<NukeCodePaperSystem>().SendNukeCodes(uid.Value);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
            {
                return CompletionResult.Empty;
            }

            var stations = new List<CompletionOption>();
            var query = _entityManager.EntityQueryEnumerator<StationDataComponent>();
            while (query.MoveNext(out var uid, out var stationData))
            {
                var meta = _entityManager.GetComponent<MetaDataComponent>(uid);

                stations.Add(new CompletionOption(uid.ToString(), meta.EntityName));
            }

            return CompletionResult.FromHintOptions(stations, null);
        }
    }
}
