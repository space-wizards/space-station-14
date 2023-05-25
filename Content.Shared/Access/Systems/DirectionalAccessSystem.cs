using System.Linq;
using Content.Shared.Access.Components;

namespace Content.Shared.Access.Systems;

public sealed class DirectionalAccessSystem : EntitySystem
{
    /// <summary>
    /// Gets absolute positions for an entity that requires access and target entity
    /// Calculates the relative cardinal direction (N, E, S, W)
    /// then compares it with the readers directions list to see if it is allowed.
    /// </summary>
    /// <param name="targetUid">The entity that targeted for an access.</param>
    /// <param name="requesterUid">The entity that wants an access.</param>
    /// <param name="reader">A reader from a targeted entity</param>
    public bool IsDirectionAllowed(EntityUid targetUid, EntityUid requesterUid, DirectionalAccessComponent reader)
    {

        if (!TryComp<TransformComponent>(targetUid, out var targetTransformComponent) || !TryComp<TransformComponent>(requesterUid, out var requesterTransformComponent))
            return false;

        var distanceVector = requesterTransformComponent.Coordinates.Position - targetTransformComponent.Coordinates.Position;
        var accessDirection = distanceVector.GetDir();

        return reader.DirectionsList.Any(x => accessDirection.ToString().Contains(x.ToString()));
    }
}
