using Content.Shared.Implants.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public sealed class ReplacementImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplacementImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(Entity<ReplacementImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (!_container.TryGetContainer(args.Implanted, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (implant == ent.Owner)
                continue; // don't delete the replacement

            if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, implant))
                PredictedQueueDel(implant);
        }

    }
}
