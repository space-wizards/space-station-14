using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Procedural.Populators.Debris;

[ImplicitDataDefinitionForInheritors]
public abstract class DebrisPopulator
{
    public abstract void Populate(EntityUid gridEnt, IMapGrid grid);
}
