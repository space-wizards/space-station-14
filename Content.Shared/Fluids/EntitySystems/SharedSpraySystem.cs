using Content.Shared.Fluids.Components;
using Robust.Shared.Map;

namespace Content.Shared.Fluids.EntitySystems;

public abstract class SharedSpaySystem : EntitySystem
{
    public virtual void Spray(Entity<SprayComponent> entity, EntityUid user, MapCoordinates mapcoord)
    {
        // do nothing!
    }
}
