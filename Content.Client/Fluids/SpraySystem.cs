using Content.Shared.Fluids.Components;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.Map;

namespace Content.Client.Fluids;

public sealed class SpraySystem : SharedSpaySystem
{
    public override void Spray(Entity<SprayComponent> entity, EntityUid user, MapCoordinates mapcoord)
    {
        // nothing on client
    }
}
