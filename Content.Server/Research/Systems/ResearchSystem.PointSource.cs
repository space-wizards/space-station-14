using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnGetPointsPerSecond(EntityUid uid, ResearchPointSourceComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (CanProduce(component))
            args.Points += component.PointsPerSecond;
    }

    public bool CanProduce(ResearchPointSourceComponent component)
    {
        return component.Active && this.IsPowered(component.Owner, EntityManager);
    }
}
