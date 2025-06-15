using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed class DeleteComponent : IConsoleCommand
    {
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "deletecomponent";
        public string Description => "Deletes all instances of the specified component.";
        public string Help => $"Usage: {Command} <name>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.WriteLine($"Not enough arguments.\n{Help}");
                    break;
                default:
                    var name = string.Join(" ", args);

                    if (!_compFactory.TryGetRegistration(name, out var registration))
                    {
                        shell.WriteLine($"No component exists with name {name}.");
                        break;
                    }

                    var componentType = registration.Type;
                    var components = _entityManager.GetAllComponents(componentType, true);

                    var i = 0;

                    foreach (var (uid, component) in components)
                    {
                        _entityManager.RemoveComponent(uid, component);
                        i++;
                    }

                    shell.WriteLine($"Removed {i} components with name {name}.");

                    break;
            }
        }
    }
}
