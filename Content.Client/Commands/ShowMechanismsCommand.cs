using Content.Shared.Body.Organ;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class ShowMechanismsCommand : IConsoleCommand
    {
        public string Command => "showmechanisms";
        public string Description => Loc.GetString("show-mechanisms-command-description");
        public string Help => Loc.GetString("show-mechanisms-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var query = entityManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

            while (query.MoveNext(out _, out var sprite))
            {
                sprite.ContainerOccluded = false;
            }

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("showcontainedcontext");
        }
    }
}
