using Content.Shared.Body.Components;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
            var mechanisms = entityManager.EntityQuery<MechanismComponent>(true);

            foreach (var mechanism in mechanisms)
            {
                if (!entityManager.TryGetComponent(mechanism.Owner, out SpriteComponent? sprite))
                {
                    continue;
                }

                sprite.ContainerOccluded = false;

                var tempParent = mechanism.Owner;
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
