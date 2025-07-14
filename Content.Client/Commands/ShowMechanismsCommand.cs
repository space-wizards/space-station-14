using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class ShowMechanismsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override string Command => "showmechanisms";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var query = EntityManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _spriteSystem.SetContainerOccluded((uid, sprite), false);
        }
    }
}
