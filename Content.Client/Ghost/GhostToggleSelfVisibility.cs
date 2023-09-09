using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client.Ghost;

public sealed class GhostToggleSelfVisibility : IConsoleCommand
{
    public string Command => "toggle_self_visibility";
    public string Description => "Toggles visibility of self ghost on your own view.";
    public string Help => "toggle_self_visibility";
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

        spriteComponent.Visible = !spriteComponent.Visible;
    }
}
