using Content.Shared.SpaceArena.Components;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SpaceArena;

public sealed partial class SpaceArenaLobbyHudSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;

    public event Action<bool>? ReturnToHubAvailableChanged;

    public bool IsSpectating { get; private set; }
    public bool CanLeaveMatch { get; private set; }
    public bool CanReturnToHub => IsSpectating || CanLeaveMatch;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceArenaSpectatorComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SpaceArenaSpectatorComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SpaceArenaSpectatorComponent, ComponentStartup>(OnSpectatorStartup);
        SubscribeLocalEvent<SpaceArenaSpectatorComponent, ComponentShutdown>(OnSpectatorShutdown);
        SubscribeLocalEvent<SpaceArenaVoluntaryLeaveComponent, LocalPlayerAttachedEvent>(OnMatchPlayerAttached);
        SubscribeLocalEvent<SpaceArenaVoluntaryLeaveComponent, LocalPlayerDetachedEvent>(OnMatchPlayerDetached);
        SubscribeLocalEvent<SpaceArenaVoluntaryLeaveComponent, ComponentStartup>(OnVoluntaryLeaveStartup);
        SubscribeLocalEvent<SpaceArenaVoluntaryLeaveComponent, ComponentShutdown>(OnVoluntaryLeaveShutdown);
    }

    public void RequestOpenLobby()
    {
        RaiseNetworkEvent(new SpaceArenaOpenLobbyRequest());
    }

    public void RequestLeaveSpectating()
    {
        RaiseNetworkEvent(new SpaceArenaLeaveSpectatingRequest());
    }

    public void RequestLeaveMatch()
    {
        RaiseNetworkEvent(new SpaceArenaLeaveMatchRequest());
    }

    private void OnPlayerAttached(
        Entity<SpaceArenaSpectatorComponent> entity,
        ref LocalPlayerAttachedEvent args)
    {
        SetSpectating(true);
    }

    private void OnPlayerDetached(
        Entity<SpaceArenaSpectatorComponent> entity,
        ref LocalPlayerDetachedEvent args)
    {
        SetSpectating(false);
    }

    private void OnSpectatorStartup(
        Entity<SpaceArenaSpectatorComponent> entity,
        ref ComponentStartup args)
    {
        if (_players.LocalEntity == entity.Owner)
            SetSpectating(true);
    }

    private void OnSpectatorShutdown(
        Entity<SpaceArenaSpectatorComponent> entity,
        ref ComponentShutdown args)
    {
        if (_players.LocalEntity == entity.Owner)
            SetSpectating(false);
    }

    private void OnMatchPlayerAttached(
        Entity<SpaceArenaVoluntaryLeaveComponent> entity,
        ref LocalPlayerAttachedEvent args)
    {
        SetCanLeaveMatch(true);
    }

    private void OnMatchPlayerDetached(
        Entity<SpaceArenaVoluntaryLeaveComponent> entity,
        ref LocalPlayerDetachedEvent args)
    {
        SetCanLeaveMatch(false);
    }

    private void OnVoluntaryLeaveStartup(
        Entity<SpaceArenaVoluntaryLeaveComponent> entity,
        ref ComponentStartup args)
    {
        if (_players.LocalEntity == entity.Owner)
            SetCanLeaveMatch(true);
    }

    private void OnVoluntaryLeaveShutdown(
        Entity<SpaceArenaVoluntaryLeaveComponent> entity,
        ref ComponentShutdown args)
    {
        if (_players.LocalEntity == entity.Owner)
            SetCanLeaveMatch(false);
    }

    private void SetSpectating(bool value)
    {
        if (IsSpectating == value)
            return;

        IsSpectating = value;
        ReturnToHubAvailableChanged?.Invoke(CanReturnToHub);
    }

    private void SetCanLeaveMatch(bool value)
    {
        if (CanLeaveMatch == value)
            return;

        CanLeaveMatch = value;
        ReturnToHubAvailableChanged?.Invoke(CanReturnToHub);
    }
}
