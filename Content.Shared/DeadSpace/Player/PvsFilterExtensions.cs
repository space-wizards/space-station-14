// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.DeadSpace.Player;

public static class PvsFilterExtensions
{
    /// <summary>
    /// Adds sessions whose subscribed remote views are in range of an event.
    /// Entity PVS already treats these subscriptions as viewers, while <see cref="Filter.AddPlayersByPvs(MapCoordinates, float, IEntityManager, ISharedPlayerManager, IConfigurationManager)"/>
    /// only checks the session's attached entity.
    /// </summary>
    public static Filter AddPlayersByViewSubscriptions(
        this Filter filter,
        MapCoordinates origin,
        float rangeMultiplier = 2f,
        IEntityManager? entityManager = null,
        ISharedPlayerManager? playerManager = null,
        IConfigurationManager? configManager = null)
    {
        IoCManager.Resolve(ref entityManager, ref playerManager, ref configManager);

        if (!configManager.GetCVar(CVars.NetPVS))
            return filter.AddAllPlayers(playerManager);

        var baseRange = configManager.GetCVar(CVars.NetMaxUpdateRange) * rangeMultiplier;
        var transformQuery = entityManager.GetEntityQuery<TransformComponent>();
        var transformSystem = entityManager.System<SharedTransformSystem>();

        foreach (var session in playerManager.NetworkedSessions)
        {
            foreach (var view in session.ViewSubscriptions)
            {
                if (!transformQuery.TryGetComponent(view, out var transform))
                    continue;

                var viewCoordinates = transformSystem.GetMapCoordinates(view, transform);
                if (viewCoordinates.MapId != origin.MapId)
                    continue;

                var scale = 1f;
                if (entityManager.TryGetComponent(view, out EyeComponent? eye))
                {
                    viewCoordinates = viewCoordinates.Offset(eye.Offset);
                    scale = MathF.Max(eye.PvsScale, 0.1f);
                }

                var range = baseRange * scale;
                if ((viewCoordinates.Position - origin.Position).LengthSquared() >= range * range)
                    continue;

                filter.AddPlayer(session);
                break;
            }
        }

        return filter;
    }
}
