using Content.Shared.Body.Organ;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class ShowMechanismsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // ReSharper disable once StringLiteralTypo
    public const string CommandName = "showmechanisms";
    public string Command => CommandName;
    public string Description => Loc.GetString("show-mechanisms-command-description");
    public string Help => Loc.GetString("show-mechanisms-command-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var query = _entManager.AllEntityQueryEnumerator<OrganComponent, SpriteComponent>();

        while (query.MoveNext(out _, out var sprite))
        {
            sprite.ContainerOccluded = false;
        }
    }
}
