using Content.Shared.Fluids.Components;
using Robust.Shared.Map;

namespace Content.Shared.Fluids.EntitySystems;

public abstract class SharedSpaySystem : EntitySystem
{
    /// <summary>
    /// Spray starting from the entity, to the given coordinates. If the user is supplied, will give them failure
    /// popups and will also push them in space.
    /// </summary>
    /// <param name="entity">Entity that is spraying.</param>
    /// <param name="mapcoord">The coordinates being aimed at.</param>
    /// <param name="user">The user that is using the spraying device.</param>
    public virtual void Spray(Entity<SprayComponent> entity, MapCoordinates mapcoord, EntityUid? user = null)
    {
        // do nothing!
    }

    /// <summary>
    /// Spray starting from the entity and facing the direction its pointing.
    /// </summary>
    /// <param name="entity">Entity that is spraying.</param>
    /// <param name="user">User that is using the spraying device.</param>
    public virtual void Spray(Entity<SprayComponent> entity, EntityUid? user = null)
    {
        // do nothing!
    }
}
