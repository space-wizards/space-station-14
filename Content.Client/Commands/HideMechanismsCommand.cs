using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Client.Commands;

public sealed class HideMechanismsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override string Command => "hidemechanisms";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var containerSys = _entityManager.System<SharedContainerSystem>();
        var query = _entityManager.AllEntityQueryEnumerator<OrganComponent>();

        _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!_entityManager.TryGetComponent(uid, out SpriteComponent? sprite))
            {
                continue;
            }

            sprite.ContainerOccluded = false;

            if (!_xformQuery.TryGetComponent(uid, out var xform))
                return;
            var tempParent = uid;

            while (containerSys.TryGetContainingContainer(xform.ParentUid, tempParent,out var container))
            {
                if (!container.ShowContents)
                {
                    sprite.ContainerOccluded = true;
                    break;
                }

                tempParent = container.Owner;

                if (!_xformQuery.TryGetComponent(tempParent, out xform))
                    break;
            }
        }
    }
}
