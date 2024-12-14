using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Client.Commands;

public sealed class HideMechanismsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "hidemechanisms";

    public override string Description => LocalizationManager.GetString($"cmd-{Command}-desc", ("showMechanismsCommand", ShowMechanismsCommand.CommandName));

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
            while (containerSys.TryGetContainingContainer((tempParent, null, null), out var container))
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
