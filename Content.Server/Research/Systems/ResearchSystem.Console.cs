using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Robust.Server.Player;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleServerSyncMessage>(OnConsoleSync);
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleServerSelectionMessage>(OnConsoleSelect);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
    }

    private void OnConsoleSelect(EntityUid uid, ResearchConsoleComponent component, ConsoleServerSelectionMessage args)
    {
        if (!HasComp<TechnologyDatabaseComponent>(uid) || !this.IsPowered(uid, EntityManager))
            return;

        _uiSystem.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
    }

    private void OnConsoleSync(EntityUid uid, ResearchConsoleComponent component, ConsoleServerSyncMessage args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        SyncClientWithServer(uid);
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!UnlockTechnology(uid, args.Id))
            return;

        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        ResearchConsoleBoundInterfaceState state;

        if (TryGetClientServer(uid, out var server, out var serverComponent, clientComponent))
        {
            var points = clientComponent.ConnectedToServer ? serverComponent.Points : 0;
            var pointsPerSecond = clientComponent.ConnectedToServer ? PointsPerSecond(server.Value, serverComponent) : 0;
            state = new ResearchConsoleBoundInterfaceState(points, pointsPerSecond);
        }
        else
        {
            state = new ResearchConsoleBoundInterfaceState(default, default);
        }

        _uiSystem.TrySetUiState(component.Owner, ResearchConsoleUiKey.Key, state);
    }

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }
}
