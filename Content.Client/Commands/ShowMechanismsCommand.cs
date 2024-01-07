using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class ShowMechanismsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public const string CommandName = "showmechanisms";

    public override string Command => CommandName;

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var query = _entManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

        while (query.MoveNext(out _, out var sprite))
        {
            sprite.ContainerOccluded = false;
        }
    }
}
