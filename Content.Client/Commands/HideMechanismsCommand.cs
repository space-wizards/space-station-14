using Content.Shared.Body.Organ;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Client.Commands
{
    public sealed class HideMechanismsCommand : IConsoleCommand
    {
        public string Command => "hidemechanisms";
        public string Description => $"Reverts the effects of {ShowMechanismsCommand.CommandName}";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var containerSys = entityManager.System<SharedContainerSystem>();
            var query = entityManager.AllEntityQueryEnumerator<OrganComponent>();

            while (query.MoveNext(out var uid, out _))
            {
                if (!entityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                {
                    continue;
                }

                sprite.ContainerOccluded = false;

                var tempParent = uid;
                while (containerSys.TryGetContainingContainer(tempParent, out var container))
                {
                    if (!container.ShowContents)
                    {
                        sprite.ContainerOccluded = true;
                        break;
                    }

                    tempParent = container.Owner;
                }
            }

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("hidecontainedcontext");
        }
    }
}
