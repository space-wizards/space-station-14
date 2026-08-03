using System.Linq;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Containers;

namespace Content.Shared.Temperature.Systems;

public sealed partial class HeatContainerQuerySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ContainerManagerComponent, QueryForHeatContainerEvent>(QueryForHeatContainer);
        base.Initialize();
    }

    private void QueryForHeatContainer(EntityUid uid,
        ContainerManagerComponent component,
        QueryForHeatContainerEvent args)
    {
        if (args.Resolved)
            return;
        QueryForHeatContainerEvent subQuery = new(null);

        foreach (var containedEntity in component.Containers.Values.SelectMany(e => e.ContainedEntities).Distinct())
        {
            RaiseLocalEvent(containedEntity, ref subQuery);
        }

        args.Responses.AddRange(subQuery.Responses);
    }
}
