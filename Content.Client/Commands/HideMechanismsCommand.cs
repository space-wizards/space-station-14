using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Client.Commands;

public sealed class HideMechanismsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override string Command => "hidemechanisms";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var query = EntityManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _spriteSystem.SetContainerOccluded((uid, sprite), false);

            var tempParent = uid;
            while (_containerSystem.TryGetContainingContainer((tempParent, null, null), out var container))
            {
                if (!container.ShowContents)
                {
                    _spriteSystem.SetContainerOccluded((uid, sprite), true);
                    break;
                }

                tempParent = container.Owner;
            }
        }
    }
}
