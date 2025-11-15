using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddAtmosCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "addatmos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var eNet) || !_entities.TryGetEntity(eNet, out var euid))
            {
                shell.WriteError(Loc.GetString("cmd-addatmos-parse-failed", ("arg", args[0])));
                return;
            }

            if (!_entities.HasComponent<MapGridComponent>(euid))
            {
                shell.WriteError(Loc.GetString("cmd-addatmos-not-grid", ("euid", euid)));
                return;
            }

            var atmos = _entities.EntitySysManager.GetEntitySystem<AtmosphereSystem>();

            if (atmos.HasAtmosphere(euid.Value))
            {
                shell.WriteLine(Loc.GetString("cmd-addatmos-already-has-atmos"));
                return;
            }

            _entities.AddComponent<GridAtmosphereComponent>(euid.Value);

            shell.WriteLine(Loc.GetString("cmd-addatmos-added", ("grid", euid)));
        }
    }
}
