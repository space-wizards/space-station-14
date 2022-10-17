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
            var organs = entityManager.EntityQuery<OrganComponent>(true);

            foreach (var part in organs)
            {
                if (!entityManager.TryGetComponent(part.Owner, out SpriteComponent? sprite))
                {
                    continue;
                }

                sprite.ContainerOccluded = false;

                var tempParent = part.Owner;
                while (tempParent.TryGetContainer(out var container))
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
