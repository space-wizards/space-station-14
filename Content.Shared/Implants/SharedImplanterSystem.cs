using System.Linq;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string ImplanterSlotId = "implanter_slot";
    public const string ImplantSlotId = "implantcontainer";

    public override void Initialize()
    {
        base.Initialize();

        //TODO: See if you need to add anything else here
    }

    //Instantly implant something and add all necessary components and containers.
    public void Implant(EntityUid implanter, EntityUid target)
    {
        if (!_container.TryGetContainer(implanter, ImplanterSlotId, out var container))
            return;

        var implant = container.ContainedEntities.FirstOrDefault();

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        if (!HasComp<ImplantedComponent>(target))
            EnsureComp<ImplantedComponent>(target);

        var implantContainer = _container.EnsureContainer<Container>(target, ImplantSlotId);
        implantComp.EntityUid = target;
        container.Remove(implant);
        implantContainer.Insert(implant);
    }
}
