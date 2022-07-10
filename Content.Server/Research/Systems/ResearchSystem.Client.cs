using Content.Server.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<ResearchClientComponent, ComponentStartup>(OnClientStartup);
        SubscribeLocalEvent<ResearchClientComponent, ComponentShutdown>(OnClientShutdown);
        SubscribeLocalEvent<ResearchClientComponent, BoundUIOpenedEvent>(OnClientUIOpen);

        SubscribeLocalEvent<ResearchClientComponent, ResearchClientSyncMessage>(OnClientSyncMessage);
        SubscribeLocalEvent<ResearchClientComponent, ResearchClientServerSelectedMessage>(OnClientSelected);
        SubscribeLocalEvent<ResearchClientComponent, ResearchClientServerDeselectedMessage>(OnClientDeselected);
    }

    #region UI

    private void OnClientDeselected(EntityUid uid, ResearchClientComponent component, ResearchClientServerDeselectedMessage args)
    {
        UnregisterClientServer(component);
        UpdateClientInterface(component);

        if (TryComp<ResearchConsoleComponent>(uid, out var console))
        {
            UpdateConsoleInterface(console, component);
        }
    }

    private void OnClientSelected(EntityUid uid, ResearchClientComponent component, ResearchClientServerSelectedMessage args)
    {
        UnregisterClientServer(component);
        RegisterClientServer(component, GetServerById(args.ServerId));
        UpdateClientInterface(component);

        if (TryComp<ResearchConsoleComponent>(uid, out var console))
        {
            UpdateConsoleInterface(console, component);
        }
    }

    private void OnClientSyncMessage(EntityUid uid, ResearchClientComponent component, ResearchClientSyncMessage args)
    {
        UpdateClientInterface(component);
    }

    #endregion

    private void OnClientStartup(EntityUid uid, ResearchClientComponent component, ComponentStartup args)
    {
        if (Servers.Count > 0)
            RegisterClientServer(component, Servers[0]);
    }

    private void OnClientShutdown(EntityUid uid, ResearchClientComponent component, ComponentShutdown args)
    {
        UnregisterClientServer(component);
    }

    private void OnClientUIOpen(EntityUid uid, ResearchClientComponent component, BoundUIOpenedEvent args)
    {
        UpdateClientInterface(component);
    }

    private void UpdateClientInterface(ResearchClientComponent component)
    {
        var state = new ResearchClientBoundInterfaceState(Servers.Count, GetServerNames(),
            GetServerIds(), component.ConnectedToServer ? component.Server!.Id : -1);

        _uiSystem.GetUiOrNull(component.Owner, ResearchClientUiKey.Key)?.SetState(state);
    }

    private bool RegisterClientServer(ResearchClientComponent component, ResearchServerComponent? server = null)
    {
        if (server == null) return false;
        return RegisterServerClient(server, component);
    }

    private void UnregisterClientServer(ResearchClientComponent component)
    {
        if (component.Server == null) return;

        UnregisterServerClient(component.Server, component);
    }
}
