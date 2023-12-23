using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Client.Commands;

public sealed class HideMechanismsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "hidemechanisms";
    public string Description => Loc.GetString("hide-mechanisms-command-description", ("showMechanismsCommand", ShowMechanismsCommand.CommandName));
    public string Help => Loc.GetString("hide-mechanisms-command-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var containerSys = _entityManager.System<SharedContainerSystem>();
        var query = _entityManager.AllEntityQueryEnumerator<OrganComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!_entityManager.TryGetComponent(uid, out SpriteComponent? sprite))
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
    }
}
