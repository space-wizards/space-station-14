using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Ghost;

public sealed class GhostToggleSelfVisibility : IConsoleCommand
{
    public string Command => "toggleselfghost";
    public string Description => "Toggles seeing your own ghost.";
    public string Help => "toggleselfghost";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var attachedEntity = shell.Player?.AttachedEntity;
        if (!attachedEntity.HasValue)
            return;

        var entityManager = IoCManager.Resolve<IEntityManager>();
        if (!entityManager.HasComponent<GhostComponent>(attachedEntity))
        {
            shell.WriteError("Entity must be a ghost.");
            return;
        }

        if (!entityManager.TryGetComponent(attachedEntity, out SpriteComponent? spriteComponent))
            return;

        var spriteSys = entityManager.System<SpriteSystem>();
        spriteSys.SetVisible((attachedEntity.Value, spriteComponent), !spriteComponent.Visible);
    }
}
