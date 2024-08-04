using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class ShowMechanismsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "showmechanisms";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var query = _entManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

        while (query.MoveNext(out _, out var sprite))
        {
            sprite.ContainerOccluded = false;
        }
    }
}
