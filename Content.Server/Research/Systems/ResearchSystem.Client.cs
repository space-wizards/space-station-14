using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<ResearchClientComponent, MapInitEvent>(OnClientMapInit);
        SubscribeLocalEvent<ResearchClientComponent, ComponentShutdown>(OnClientShutdown);
        SubscribeLocalEvent<ResearchClientComponent, BoundUIOpenedEvent>(OnClientUIOpen);
        SubscribeLocalEvent<ResearchClientComponent, ConsoleServerSelectionMessage>(OnConsoleSelect);
        SubscribeLocalEvent<ResearchClientComponent, AnchorStateChangedEvent>(OnClientAnchorStateChanged);

        SubscribeLocalEvent<ResearchClientComponent, ResearchClientSyncMessage>(OnClientSyncMessage);
        SubscribeLocalEvent<ResearchClientComponent, ResearchClientServerSelectedMessage>(OnClientSelected);
        SubscribeLocalEvent<ResearchClientComponent, ResearchClientServerDeselectedMessage>(OnClientDeselected);
        SubscribeLocalEvent<ResearchClientComponent, ResearchRegistrationChangedEvent>(OnClientRegistrationChanged);
    }

    #region UI

    private void OnClientSelected(EntityUid uid, ResearchClientComponent component, ResearchClientServerSelectedMessage args)
    {
        if (!TryGetServerById(uid, args.ServerId, out var serveruid, out var serverComponent))
            return;

        // Validate that we can access this server.
        if (!GetServers(uid).Contains((serveruid.Value, serverComponent)))
            return;

        UnregisterClient(uid, component);
        RegisterClient(uid, serveruid.Value, component, serverComponent);
    }

    private void OnClientDeselected(EntityUid uid, ResearchClientComponent component, ResearchClientServerDeselectedMessage args)
    {
        UnregisterClient(uid, clientComponent: component);
    }

    private void OnClientSyncMessage(EntityUid uid, ResearchClientComponent component, ResearchClientSyncMessage args)
    {
        UpdateClientInterface(uid, component);
    }

    private void OnConsoleSelect(EntityUid uid, ResearchClientComponent component, ConsoleServerSelectionMessage args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        _uiSystem.TryToggleUi(uid, ResearchClientUiKey.Key, args.Actor);
    }
    #endregion

    private void OnClientRegistrationChanged(EntityUid uid, ResearchClientComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateClientInterface(uid, component);
    }

    private void OnClientMapInit(EntityUid uid, ResearchClientComponent component, MapInitEvent args)
    {
        var allServers = GetServers(uid).ToList();

        if (allServers.Count > 0)
            RegisterClient(uid, allServers[0], component, allServers[0]);
    }

    private void OnClientShutdown(EntityUid uid, ResearchClientComponent component, ComponentShutdown args)
    {
        UnregisterClient(uid, component);
    }

    private void OnClientUIOpen(EntityUid uid, ResearchClientComponent component, BoundUIOpenedEvent args)
    {
        UpdateClientInterface(uid, component);
    }

    private void OnClientAnchorStateChanged(Entity<ResearchClientComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            if (ent.Comp.Server is not null)
                return;

            var allServers = GetServers(ent).ToList();

            if (allServers.Count > 0)
                RegisterClient(ent, allServers[0], ent, allServers[0]);
        }
        else
        {
            UnregisterClient(ent, ent.Comp);
        }
    }

    private void UpdateClientInterface(EntityUid uid, ResearchClientComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        TryGetClientServer(uid, out _, out var serverComponent, component);

        var names = GetServerNames(uid);
        var state = new ResearchClientBoundInterfaceState(
            names.Length,
            names,
            GetServerIds(uid),
            serverComponent?.Id ?? -1);

        _uiSystem.SetUiState(uid, ResearchClientUiKey.Key, state);
    }

    /// <summary>
    /// Tries to get the server belonging to a client
    /// </summary>
    /// <param name="uid">The client</param>
    /// <param name="server">It's server. Null if false.</param>
    /// <param name="serverComponent">The server's ResearchServerComponent. Null if false</param>
    /// <param name="component">The client's Researchclient component</param>
    /// <returns>If the server was successfully retrieved.</returns>
    public bool TryGetClientServer(EntityUid uid,
        [NotNullWhen(returnValue: true)] out EntityUid? server,
        [NotNullWhen(returnValue: true)] out ResearchServerComponent? serverComponent,
        ResearchClientComponent? component = null)
    {
        server = null;
        serverComponent = null;

        if (!Resolve(uid, ref component, false))
            return false;

        if (component.Server == null)
            return false;

        if (!TryComp(component.Server, out serverComponent))
            return false;

        server = component.Server;
        return true;
    }
}
