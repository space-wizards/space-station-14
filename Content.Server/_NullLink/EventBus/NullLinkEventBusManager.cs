using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.PlayerData;
using Orleans;
using Starlight.NullLink;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.EventBus;

public sealed partial class NullLinkEventBusManager : IEventBusObserver, INullLinkEventBusManager
{
    private static readonly TimeSpan _grainDelay = TimeSpan.FromSeconds(180);
    private Timer? _resubscribeTimer;

    [Dependency] private readonly IActorRouter _actors = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INullLinkPlayerManager _players = default!;

    private ISawmill _sawmill = default!;
    private readonly ConcurrentQueue<BaseEvent> _eventQueue = [];

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("NullLink event bus");
        _actors.OnConnected += OnNullLinkConnected;
        _resubscribeTimer = new Timer(
            async _ => await Resubscribe(),
            null,
            dueTime: TimeSpan.Zero,
            period: _grainDelay);
    }


    public void Shutdown()
    {
        _resubscribeTimer?.Dispose();
        _actors.OnConnected -= OnNullLinkConnected;

        if (!_actors.Enabled
            || !_actors.TryGetServerGrain(out var serverGrain)
            || !_actors.TryCreateObjectReference<IEventBusObserver>(this, out var eventBusObserver) 
            || eventBusObserver is null)
            return;

        _ = serverGrain.UnsubscribeEventBus(eventBusObserver);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out BaseEvent result)
        => _eventQueue.TryDequeue(out result);

    public ValueTask OnEventReceived<T>(T @event) where T : BaseEvent 
        => @event switch
        {
            PlayerRolesSyncEvent playerRolesSyncEvent
                => _players.SyncRoles(playerRolesSyncEvent),

            RolesChangedEvent rolesChangedEvent
                => _players.UpdateRoles(rolesChangedEvent),

            BaseEvent baseEvent
                => Enqueue(baseEvent),
        };

    // ValueTask is kind of a hint that this might be a different, unknown thread.
    // And it also lets me use a clean and convenient switch.
    private ValueTask Enqueue(BaseEvent baseEvent)
    {
        _eventQueue.Enqueue(baseEvent);
        return ValueTask.CompletedTask;
    }
    private void OnNullLinkConnected() => _ = Resubscribe();

    private async ValueTask Resubscribe()
    {
        if (!_actors.Enabled)
            return;

        if (!_actors.TryGetServerGrain(out var serverGrain))
        {
            _sawmill.Log(LogLevel.Warning, "Failed to get server grain for resubscription.");
            return;
        }
        if (!_actors.TryCreateObjectReference<IEventBusObserver>(this, out var eventBusObserver) || eventBusObserver is null)
        {
            _sawmill.Log(LogLevel.Warning, "Failed to create event bus observer reference.");
            return;
        }

        await serverGrain.ResubscribeEventBus(eventBusObserver);
    }
}
