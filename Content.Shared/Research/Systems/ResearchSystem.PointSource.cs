using Content.Shared.Research.Components;

namespace Content.Shared.Research.Systems;

public partial class ResearchSystem
{
    private void InitializeSource()
    {
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (CanProduce(source))
            args.Points += source.Comp.PointsPerSecond;
    }

    public bool CanProduce(Entity<ResearchPointSourceComponent> source)
    {
        return source.Comp.Active && _power.IsPowered(source.Owner);
    }
}
