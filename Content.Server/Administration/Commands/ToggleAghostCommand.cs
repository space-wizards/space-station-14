using Content.Server.Ghost.Components;
using Content.Server.Visible;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleAghostVisibility : IConsoleCommand
{
    public string Command => "toggleaghost";
    public string Description => "Shows or hides your aghost from other players";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            shell.WriteLine("You can only toggle ghost visibility on a client.");

        var entityManager = IoCManager.Resolve<IEntityManager>();

        var uid = shell.Player?.AttachedEntity;
        if (uid == null
            || !entityManager.HasComponent<GhostComponent>(uid)
            || !entityManager.TryGetComponent<EyeComponent>(uid, out var eyeComponent)
            || !entityManager.TryGetComponent(uid, out VisibilityComponent? visibility))
            return;

        var _visibilitySystem = entityManager.EntitySysManager.GetEntitySystem<VisibilitySystem>();

        if (visibility.Layer == (int) VisibilityFlags.Ghost)
            _visibilitySystem.SetLayer(visibility, (int) VisibilityFlags.Aghost);
        else
            _visibilitySystem.SetLayer(visibility, (int) VisibilityFlags.Ghost);


    }
}
