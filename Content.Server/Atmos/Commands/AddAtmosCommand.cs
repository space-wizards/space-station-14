using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddAtmosCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        public override string Command => "addatmos";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString($"shell-need-exactly-one-argument"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var eNet) || !EntityManager.TryGetEntity(eNet, out var euid))
            {
                shell.WriteError(Loc.GetString($"shell-invalid-entity-uid", ("uid", args[0])));
                return;
            }

            if (!EntityManager.HasComponent<MapGridComponent>(euid))
            {
                shell.WriteError(Loc.GetString($"shell-invalid-grid-id-specific", ("grid", euid)));
                return;
            }

            if (_atmosSystem.HasAtmosphere(euid.Value))
            {
                shell.WriteLine(Loc.GetString($"cmd-addatmos-grid-already-has-atmos"));
                return;
            }

            EntityManager.AddComponent<GridAtmosphereComponent>(euid.Value);

            shell.WriteLine(Loc.GetString($"cmd-addatmos-success", ("grid", euid)));
        }
    }
}
