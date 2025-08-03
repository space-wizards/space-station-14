using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Zombies;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Zombies;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ZombifyCommand : ToolshedCommand
{
    private ZombieSystem? _zombieSystem;

    [CommandImplementation("infect")]
    public void Infect([PipedArgument] IEnumerable<EntityUid> input)
    {
        _zombieSystem ??= EntityManager.System<ZombieSystem>();

        foreach (var entity in input)
        {
            _zombieSystem.ZombifyEntity(entity);
        }
    }

    [CommandImplementation("infect")]
    public void Infect(EntityUid input)
    {
        _zombieSystem ??= EntityManager.System<ZombieSystem>();

        _zombieSystem.ZombifyEntity(input);
    }
}
