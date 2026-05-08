using Content.Shared.Guidebook;
using Robust.Client;

namespace Content.Client.Guidebook;

/// <summary>
/// Client system for storing and retrieving values extracted from entity prototypes
/// for display in the guidebook (<see cref="RichText.ProtodataTag"/>).
/// Requests data from the server after the player has fully joined.
/// Can also be pushed new data when the server reloads prototypes.
/// </summary>
public sealed class GuidebookDataSystem : EntitySystem
{
    [Dependency] private readonly IBaseClient _client = default!;

    private GuidebookData? _data;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateGuidebookDataEvent>(OnServerUpdated);
        _client.PlayerJoinedServer += OnPlayerJoinedServer;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _client.PlayerJoinedServer -= OnPlayerJoinedServer;
    }

    private void OnPlayerJoinedServer(object? sender, PlayerEventArgs args)
    {
        RaiseNetworkEvent(new RequestGuidebookDataEvent());
    }

    private void OnServerUpdated(UpdateGuidebookDataEvent args)
    {
        // Got new data from the server, either in response to our request, or because prototypes reloaded on the server
        _data = args.Data;
        _data.Freeze();
    }

    /// <summary>
    /// Attempts to retrieve a value using the given identifiers.
    /// See <see cref="GuidebookData.TryGetValue"/> for more information.
    /// </summary>
    public bool TryGetValue(string prototype, string component, string field, out object? value)
    {
        if (_data == null)
        {
            value = null;
            return false;
        }
        return _data.TryGetValue(prototype, component, field, out value);
    }
}
