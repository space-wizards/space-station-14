using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Ghost;

public sealed class GhostToggleSelfVisibility : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public string Command => "toggleselfghost";
    public string Description => "Toggles seeing your own ghost.";
    public string Help => "toggleselfghost";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var attachedEntity = shell.Player?.AttachedEntity;
        if (!attachedEntity.HasValue)
            return;

        if (!_ent.HasComponent<GhostComponent>(attachedEntity))
        {
            shell.WriteError("Entity must be a ghost.");
            return;
        }

        if (!_ent.TryGetComponent(attachedEntity, out SpriteComponent? spriteComponent))
            return;

        spriteComponent.Visible = !spriteComponent.Visible;
    }
}
