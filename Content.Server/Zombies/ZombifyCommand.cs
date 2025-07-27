using Content.Server.Administration;
using Content.Shared.Administration;
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
}

[AdminCommand(AdminFlags.Fun)]
public sealed class ZombifyConsoleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ZombieSystem _zombieSystem = default!;

    public override string Command => "zombify";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !EntityManager.TryGetEntity(netEntity, out var entity))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (EntityManager.HasComponent<ZombieComponent>(entity))
        {
            shell.WriteError(Loc.GetString("cmd-zombify-target-is-already-zombified"));
            return;
        }

        if (EntityManager.HasComponent<ZombieImmuneComponent>(entity))
        {
            shell.WriteError(Loc.GetString("cmd-zombify-target-cannot-be-zombified"));
            return;
        }

        _zombieSystem.ZombifyEntity(entity.Value);
    }
}
