using Content.Shared.Body.Components;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class ShowMechanismsCommand : IConsoleCommand
    {
        public const string CommandName = "showmechanisms";

        // ReSharper disable once StringLiteralTypo
        public string Command => CommandName;
        public string Description => "Makes mechanisms visible, even when they shouldn't be.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var parts = entityManager.EntityQuery<BodyComponent>(true);

            foreach (var mechanism in parts)
            {
                if (mechanism.Organ && entityManager.TryGetComponent(mechanism.Owner, out SpriteComponent? sprite))
                {
                    sprite.ContainerOccluded = false;
                }
            }

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("showcontainedcontext");
        }
    }
}
