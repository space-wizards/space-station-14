using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Server.Player;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleServerSyncMessage>(OnConsoleSync);
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleServerSelectionMessage>(OnConsoleSelect);
    }

    private void OnConsoleSelect(EntityUid uid, ResearchConsoleComponent component, ConsoleServerSelectionMessage args)
    {
        if (!HasComp<TechnologyDatabaseComponent>(uid) ||
            !HasComp<ResearchClientComponent>(uid) ||
            !this.IsPowered(uid, EntityManager))
            return;

        _uiSystem.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
    }

    private void OnConsoleSync(EntityUid uid, ResearchConsoleComponent component, ConsoleServerSyncMessage args)
    {
        if (!TryComp<TechnologyDatabaseComponent>(uid, out var database) ||
            !HasComp<ResearchClientComponent>(uid) ||
            !this.IsPowered(uid, EntityManager))
            return;

        SyncWithServer(database);
        UpdateConsoleInterface(component);
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        if (!TryComp<TechnologyDatabaseComponent>(uid, out var database) ||
            !TryComp<ResearchClientComponent>(uid, out var client) ||
            !this.IsPowered(uid, EntityManager))
            return;

        if (!_prototypeManager.TryIndex(args.Id, out TechnologyPrototype? tech) ||
            client.Server == null ||
            !CanUnlockTechnology(client.Server, tech)) return;

        if (!UnlockTechnology(client.Server, tech)) return;

        SyncWithServer(database);
        Dirty(database);
        UpdateConsoleInterface(component);
    }

    private void UpdateConsoleInterface(ResearchConsoleComponent component, ResearchClientComponent? clientComponent = null)
    {
        ResearchConsoleBoundInterfaceState state;

        if (!Resolve(component.Owner, ref clientComponent, false) ||
            clientComponent.Server == null)
        {
            state = new ResearchConsoleBoundInterfaceState(default, default);
        }
        else
        {
            var points = clientComponent.ConnectedToServer ? clientComponent.Server.Points : 0;
            var pointsPerSecond = clientComponent.ConnectedToServer ? PointsPerSecond(clientComponent.Server) : 0;
            state = new ResearchConsoleBoundInterfaceState(points, pointsPerSecond);
        }
        _uiSystem.GetUiOrNull(component.Owner, ResearchConsoleUiKey.Key)?.SetState(state);
    }
}
