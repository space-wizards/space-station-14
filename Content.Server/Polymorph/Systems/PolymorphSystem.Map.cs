using Content.Shared.GameTicking;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    public EntityUid? PausedMap { get; private set; }

    /// <summary>
    /// Used to subscribe to the round restart event
    /// </summary>
    private void InitializeMap()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
    }

    /// <summary>
    /// Used internally to ensure a paused map that is
    /// stores polymorphed entities.
    /// </summary>
    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var newmap = _mapManager.CreateMap();
        _mapManager.SetMapPaused(newmap, true);
        PausedMap = _mapManager.GetMapEntityId(newmap);
    }

    /// <summary>
    /// Sends an entity to the paused map.
    /// </summary>
    private void SendToPausedMap(EntityUid uid, TransformComponent comp)
    {
        EnsurePausedMap();

        if (PausedMap != null)
            _transform.SetParent(uid, comp, PausedMap.Value);
    }

    /// <summary>
    /// Retrieves a paused entity (target) at the user's position
    /// </summary>
    private void RetrievePausedEntity(EntityUid user, EntityUid target)
    {
        if (Deleted(user))
            return;
        if (Deleted(target))
            return;

        var targetTransform = Transform(target);
        var userTransform = Transform(user);

        _transform.SetParent(target, targetTransform, user);
        targetTransform.Coordinates = userTransform.Coordinates;
        targetTransform.LocalRotation = userTransform.LocalRotation;

        if (_container.TryGetContainingContainer(user, out var cont))
            _container.Insert(target, cont);
    }
}
