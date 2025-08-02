using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Ghost.Commands;

public sealed class ToggleSelfGhostCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override string Command => "toggleselfghost";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.AttachedEntity is not { } attachedEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (!EntityManager.HasComponent<GhostComponent>(attachedEntity))
        {
            shell.WriteError(Loc.GetString($"cmd-toggleselfghost-must-be-ghost"));
            return;
        }

        if (!EntityManager.TryGetComponent(attachedEntity, out SpriteComponent? spriteComponent))
        {
            shell.WriteError(Loc.GetString("shell-entity-target-lacks-component", ("componentName", nameof(SpriteComponent))));
            return;
        }

        _sprite.SetVisible((attachedEntity, spriteComponent), !spriteComponent.Visible);
    }
}
