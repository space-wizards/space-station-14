using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly IComponentFactory ComponentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeNetworking();

        SubscribeLocalEvent<SharedBodyPartComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SharedBodyPartComponent, PartAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<SharedBodyPartComponent, PartRemovedFromBodyEvent>(OnRemovedFromBody);
    }

    private void OnComponentInit(EntityUid uid, SharedBodyPartComponent component, ComponentInit args)
    {
        component.MechanismContainer =
            ContainerSystem.EnsureContainer<Container>(uid, $"{ComponentFactory.GetComponentName(typeof(SharedBodyPartComponent))}-{nameof(SharedBodyPartComponent)}");
    }

    private void OnAddedToBody(EntityUid uid, SharedBodyPartComponent component, PartAddedToBodyEvent args)
    {
        var xform = Transform(uid);
        xform.LocalRotation = 0;
        xform.AttachParent(args.BodyUid);

        foreach (var mechanism in component.Mechanisms)
        {
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToBodyEvent(args.BodyUid), true);
        }
    }

    private void OnRemovedFromBody(EntityUid uid, SharedBodyPartComponent component, PartRemovedFromBodyEvent args)
    {
        if (!Deleted(uid))
        {
            Transform(uid).AttachToGridOrMap();
        }

        foreach (var mechanism in component.Mechanisms)
        {
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromBodyEvent(args.BodyUid), true);
        }
    }
}
