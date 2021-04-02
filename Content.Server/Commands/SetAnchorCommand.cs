#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class SetAnchorCommand : IConsoleCommand
    {
        public string Command => "setanchor";
        public string Description => "Sets the anchoring state of an entity.";
        public string Help => "setanchor <entity id> <value (optional)>";
        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                shell.WriteLine("Invalid number of argument!");
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteLine("Invalid argument specified!");
                return;
            }

            var entId = new EntityUid(id);

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entId, out var entity) || entity.Deleted || !entity.TryGetComponent(out AnchorableComponent? anchorable))
            {
                shell.WriteLine("Invalid entity specified!");
                return;
            }

            if (args.Length == 2)
            {
                if (!bool.TryParse(args[1], out var value))
                {
                    shell.WriteLine("Invalid argument specified!");
                    return;
                }

                if (value)
                    await anchorable.TryAnchor(default, force: true);
                else
                    await anchorable.TryUnAnchor(default, force: true);
                return;
            }

            await anchorable.TryToggleAnchor(default, force: true);
        }
    }
}
