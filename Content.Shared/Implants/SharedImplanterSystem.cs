using Content.Shared.Hands;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ContainerManagerComponent _containerManager = default!;

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

        if (args.Target == null)
            return;

        Implant(uid, args.Target.Value, component);
    }

    private void Implant(EntityUid user, EntityUid target, ImplanterComponent component)
    {
        //This works to add a container, pog
        //Use a container because someone can have multiple implants
        _container.EnsureContainer<Container>(target, "ImplantContainer", _containerManager);
    }
}
