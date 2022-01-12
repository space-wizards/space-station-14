using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem : EntitySystem
{
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
            _containerSystem.EnsureContainer<Container>(uid, $"{component.Name}-{nameof(SharedBodyPartComponent)}");
    }

    private void OnAddedToBody(EntityUid uid, SharedBodyPartComponent component, PartAddedToBodyEvent args)
    {
        var xform = Transform(uid);
        xform.LocalRotation = 0;
        xform.AttachParent(args.NewBody.Owner);

        foreach (var mechanism in component.Mechanisms)
        {
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToBodyEvent(args.NewBody));
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
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromBodyEvent(args.OldBody), false);
        }
    }

    [Dependency] protected readonly SharedContainerSystem _containerSystem = default!;
}
