using Content.Shared.Fluids.Components;
using Robust.Shared.Map;

namespace Content.Shared.Fluids.EntitySystems;

public abstract class SharedSpaySystem : EntitySystem
{
    public abstract void Spray(Entity<SprayComponent> entity, EntityUid user, MapCoordinates mapcoord);
}
