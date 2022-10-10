using System.Linq;
using Content.Shared.Hands;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string ImplanterSlotId = "implanter_slot";
    public const string ImplantSlotId = "ImplantContainer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, HandDeselectedEvent>(OnHandDeselect);
        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);

        //TODO: Add container check events to check for subdermal implant component
    }

    private void OnHandDeselect(EntityUid uid, ImplanterComponent component, HandDeselectedEvent args)
    {
        //TODO: Cancel inject attempt on others
        //TODO: move to server since it'll need to interact with doafter
    }

    private void OnImplanterAfterInteract(EntityUid uid, ImplanterComponent component, AfterInteractEvent args)
    {
        //Going to want to check for handled and such, as well as if it's a living entity and if there's a container already.
        if (args.Target == null || args.Handled)
            return;

        //Check to instant implant self but not others
        //if (args.User != args.Target) { }

        Implant(uid, args.Target.Value);

        args.Handled = true;
    }

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
