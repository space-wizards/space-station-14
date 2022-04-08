using Robust.Shared.Map;

namespace Content.Server.OuterRim.Worldgen.Populators.Debris;

[ImplicitDataDefinitionForInheritors]
public abstract class DebrisPopulator
{
    public abstract void Populate(EntityUid gridEnt, IMapGrid grid);
}
