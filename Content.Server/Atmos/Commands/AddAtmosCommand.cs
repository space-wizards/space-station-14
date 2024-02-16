using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddAtmosCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "addatmos";
        public string Description => "Adds atmos support to a grid.";
        public string Help => $"{Command} <GridId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var eNet) || !_entities.TryGetEntity(eNet, out var euid))
            {
                shell.WriteError($"Failed to parse euid '{args[0]}'.");
                return;
            }

            if (!_entities.HasComponent<MapGridComponent>(euid))
            {
                shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
                return;
            }

            var atmos = _entities.EntitySysManager.GetEntitySystem<AtmosphereSystem>();

            if (atmos.HasAtmosphere(euid.Value))
            {
                shell.WriteLine("Grid already has an atmosphere.");
                return;
            }

            _entities.AddComponent<GridAtmosphereComponent>(euid.Value);

            shell.WriteLine($"Added atmosphere to grid {euid}.");
        }
    }
}
