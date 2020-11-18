using Content.Shared.GameObjects.Components.Body.Mechanism;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Console;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class HideMechanismsCommand : IConsoleCommand
    {
        public string Command => "hidemechanisms";
        public string Description => $"Reverts the effects of {ShowMechanismsCommand.CommandName}";
        public string Help => $"{Command}";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();
            var mechanisms = componentManager.EntityQuery<IMechanism>();

            foreach (var mechanism in mechanisms)
            {
                if (!mechanism.Owner.TryGetComponent(out SpriteComponent sprite))
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

            IoCManager.Resolve<IClientConsole>().ProcessCommand("hidecontainedcontext");

            return false;
        }
    }
}
