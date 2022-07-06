using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    public bool CanProduce(ResearchPointSourceComponent component)
    {
        return component.Active && this.IsPowered(component.Owner, EntityManager);
    }
}
