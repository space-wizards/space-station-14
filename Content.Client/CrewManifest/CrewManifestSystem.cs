using Content.Client.GameTicking.Managers;
using Content.Shared.CrewManifest;

namespace Content.Client.CrewManifest;

public sealed class CrewManifestSystem : EntitySystem
{
    [Dependency] private readonly ClientGameTicker _gameTicker = default!;

    private readonly Dictionary<EntityUid, Action<CrewManifestState>> _registeredCallbacks = new();

    // public Action<CrewManifestState>? UpdateCrewManifest;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CrewManifestState>(OnCrewManifestStateReceived);
    }

    /// <summary>
    ///     Requests a crew manifest from the server.
    /// </summary>
    /// <param name="source">What type of entity we're requesting from: a station, or just a game object.</param>
    /// <param name="uid">EntityUid of the entity we're requesting the crew manifest from.</param>
    public void RequestCrewManifest(CrewManifestEntitySource source, EntityUid uid)
    {
        RaiseNetworkEvent(new RequestCrewManifestMessage(source, uid));
    }

    /// <summary>
    ///     Subscribes this callback method to the station's entity UID. <see cref="CrewManifestUi"/> has
    ///     an implementation that registers a UI window instance so it receives crew manifest updates.
    /// </summary>
    /// <param name="station">Station to listen for crew manifest state updates.</param>
    /// <param name="callback">Function that is called when a state update is received.</param>
    public void SubscribeCrewManifestUpdate(EntityUid station, Action<CrewManifestState> callback)
    {
        // sussy...
        if (_registeredCallbacks.TryGetValue(station, out var update))
        {
            update += callback;
        }
        else
        {
            _registeredCallbacks.Add(station, callback);
        }
    }

    /// <summary>
    ///     Unsubscribes this callback method from the given station.
    /// </summary>
    /// <param name="station">Station that has the callback registered.</param>
    /// <param name="callback">The callback to unregister.</param>
    public void UnsubscribeCrewManifestUpdate(EntityUid station, Action<CrewManifestState> callback)
    {
        if (!_registeredCallbacks.TryGetValue(station, out var update))
        {
            return;
        }

        update -= callback;

        if (update == null)
        {
            _registeredCallbacks.Remove(station);
        }
    }

    private void OnCrewManifestStateReceived(CrewManifestState args)
    {
        if (args.Station == null)
        {
            return;
        }

        args.StationName = _gameTicker.StationNames.TryGetValue(args.Station.Value, out var name)
            ? name
            : "unknown"; // TODO: localized string

        if (_registeredCallbacks.TryGetValue(args.Station.Value, out var update))
        {
            update.Invoke(args);
        }
    }
}
