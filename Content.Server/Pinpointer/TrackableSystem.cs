using Content.Shared.Destructible;
using Content.Shared.Pinpointer;

namespace Content.Server.Pinpointer;

public sealed class TrackableSystem : EntitySystem
{
    [Dependency] private readonly PinpointerSystem _pinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrackableComponent, ComponentRemove>(OnRemove);
    }

    /// <summary>
    /// When the component is removed, remove the entity from the target list of all entities tracking it.
    /// </summary>
    private void OnRemove(EntityUid uid, TrackableComponent component, ComponentRemove args)
    {
        foreach (var tracker in component.TrackedBy)
        {
            if (TryComp<PinpointerComponent>(tracker, out var pinpointer))
            {
                _pinpointerSystem.RemoveTarget(uid, pinpointer, tracker);
            }
        }
    }
}
