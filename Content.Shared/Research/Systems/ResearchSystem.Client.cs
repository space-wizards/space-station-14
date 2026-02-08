using Content.Shared.Research.Components;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Research.Systems;

public partial class ResearchSystem
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

    private void OnClientSelected(Entity<ResearchClientComponent> ent, ref ResearchClientServerSelectedMessage args)
    {
        if (!TryGetServerById(ent, args.ServerId, out var server))
            return;

        // Validate that we can access this server.
        if (!GetServers(ent).Contains(server.Value))
            return;

        UnregisterClient(ent);
        RegisterClient(ent.AsNullable(), server.Value.AsNullable());
    }

    private void OnClientDeselected(Entity<ResearchClientComponent> ent, ref ResearchClientServerDeselectedMessage args)
    {
        UnregisterClient(ent);
    }

    private void OnClientSyncMessage(Entity<ResearchClientComponent> ent, ref ResearchClientSyncMessage args)
    {
        UpdateClientInterface(ent);
    }

    private void OnConsoleSelect(Entity<ResearchClientComponent> ent, ref ConsoleServerSelectionMessage args)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        _uiSystem.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }
    #endregion

    private void OnClientRegistrationChanged(Entity<ResearchClientComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateClientInterface(ent);
    }

    private void OnClientMapInit(Entity<ResearchClientComponent> ent, ref MapInitEvent args)
    {
        if (GetServers(ent).FirstOrNull() is { } server)
            RegisterClient(ent.AsNullable(), server);
    }

    private void OnClientShutdown(Entity<ResearchClientComponent> ent, ref ComponentShutdown args)
    {
        UnregisterClient(ent);
    }

    private void OnClientUIOpen(Entity<ResearchClientComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateClientInterface(ent);
    }

    private void OnClientAnchorStateChanged(Entity<ResearchClientComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            if (ent.Comp.Server is not null)
                return;

            if (GetServers(ent).FirstOrNull() is { } server)
                RegisterClient(ent.AsNullable(), server);
        }
        else
        {
            UnregisterClient(ent);
        }
    }

    private void UpdateClientInterface(Entity<ResearchClientComponent> ent)
    {
        TryGetClientServer(ent, out var server);

        var names = GetServerNames(ent);
        var state = new ResearchClientBoundInterfaceState(
            names.Length,
            names,
            GetServerIds(ent),
            server?.Comp.Id ?? -1);

        _uiSystem.SetUiState(ent.Owner, ResearchClientUiKey.Key, state);
    }

    /// <summary>
    /// Tries to get the server belonging to a client
    /// </summary>
    /// <param name="ent">The client</param>
    /// <param name="server">Its server. Null if false.</param>
    /// <returns>If the server was successfully retrieved.</returns>
    public bool TryGetClientServer(Entity<ResearchClientComponent> ent,
        [NotNullWhen(returnValue: true)] out Entity<ResearchServerComponent>? server)
    {
        server = null;

        if (ent.Comp.Server is not { } serverEnt)
            return false;

        if (!TryComp<ResearchServerComponent>(ent.Comp.Server, out var serverComponent))
            return false;

        server = (serverEnt, serverComponent);
        return true;
    }
}
