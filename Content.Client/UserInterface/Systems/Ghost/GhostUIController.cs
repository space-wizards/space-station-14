using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.Ghost;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed class GhostUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    [UISystemDependency] private readonly GhostSystem? _system = default;

    private GhostGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostGui>();

    public override void OnSystemLoaded(IEntitySystem system)
    {
        base.OnSystemLoaded(system);

        switch (system)
        {
            case GhostSystem ghost:
                ghost.PlayerRemoved += OnPlayerRemoved;
                ghost.PlayerUpdated += OnPlayerUpdated;
                ghost.PlayerAttached += OnPlayerAttached;
                ghost.PlayerDetached += OnPlayerDetached;
                ghost.GhostWarpsResponse += OnWarpsResponse;
                ghost.GhostRoleCountUpdated += OnRoleCountUpdated;
                break;
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case GhostSystem ghost:
                ghost.PlayerRemoved -= OnPlayerRemoved;
                ghost.PlayerUpdated -= OnPlayerUpdated;
                ghost.PlayerAttached -= OnPlayerAttached;
                ghost.PlayerDetached -= OnPlayerDetached;
                ghost.GhostWarpsResponse -= OnWarpsResponse;
                ghost.GhostRoleCountUpdated -= OnRoleCountUpdated;
                break;
        }
    }

    private void UpdateGui()
    {
        Gui?.Update(_system?.AvailableGhostRoleCount, _system?.Player?.CanReturnToBody);
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        Gui?.Hide();
    }

    private void OnPlayerUpdated(GhostComponent component)
    {
        UpdateGui();
    }

    private void OnPlayerAttached(GhostComponent component)
    {
        if (Gui == null)
            return;

        Gui.Visible = true;
        UpdateGui();
    }

    private void OnPlayerDetached(GhostComponent component)
    {
        Gui?.Hide();
    }

    private void OnWarpsResponse(GhostWarpsResponseEvent msg)
    {
        if (Gui?.TargetWindow is not { } window)
            return;

        window.Locations = msg.Locations;
        window.Players = msg.Players;
        window.Populate();
    }

    private void OnRoleCountUpdated(GhostUpdateGhostRoleCountEvent msg)
    {
        UpdateGui();
    }

    private void PlayerClicked(EntityUid player)
    {
        var msg = new GhostWarpToTargetRequestEvent(player);
        _net.SendSystemNetworkMessage(msg);
    }

    private void LocationClicked(string location)
    {
        var msg = new GhostWarpToLocationRequestEvent(location);
        _net.SendSystemNetworkMessage(msg);
    }

    public void OnStateEntered(GameplayState state)
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed += RequestWarps;
        Gui.ReturnToBodyPressed += ReturnToBody;
        Gui.GhostRolesPressed += GhostRolesPressed;
        Gui.TargetWindow.PlayerClicked += PlayerClicked;
        Gui.TargetWindow.LocationClicked += LocationClicked;

        Gui.Visible = _system?.IsGhost ?? false;
    }

    public void OnStateExited(GameplayState state)
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed -= RequestWarps;
        Gui.ReturnToBodyPressed -= ReturnToBody;
        Gui.GhostRolesPressed -= GhostRolesPressed;
        Gui.TargetWindow.PlayerClicked -= PlayerClicked;
        Gui.TargetWindow.LocationClicked -= LocationClicked;

        Gui.Hide();
    }

    private void ReturnToBody()
    {
        _system?.ReturnToBody();
    }

    private void RequestWarps()
    {
        _system?.RequestWarps();
        Gui?.TargetWindow.Populate();
        Gui?.TargetWindow.OpenCentered();
    }

    private void GhostRolesPressed()
    {
        _system?.OpenGhostRoles();
    }
}
