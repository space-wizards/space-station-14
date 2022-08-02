using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

    public const string ContainerName = "part-mechanisms";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBodyPartComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SharedBodyPartComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<SharedBodyPartComponent, PartAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<SharedBodyPartComponent, PartRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<SharedBodyPartComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<SharedBodyPartComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedBodyPartComponent component, ComponentInit args)
    {
        component.MechanismContainer =
            ContainerSystem.EnsureContainer<Container>(uid, ContainerName);
    }

    private void OnComponentGetState(EntityUid uid, SharedBodyPartComponent component, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(component.PartType, component.Symmetry);
    }

    private void OnComponentHandleState(EntityUid uid, SharedBodyPartComponent component, ref ComponentHandleState args)
    {
        if (args.Current is BodyPartComponentState state)
        {
            component.PartType = state.PartType;
            component.Symmetry = state.Symmetry;
        }
    }

    protected virtual void OnAddedToBody(EntityUid uid, SharedBodyPartComponent component, PartAddedToBodyEvent args)
    {
        var xform = Transform(uid);
        xform.LocalRotation = 0;
        xform.AttachParent(args.Body);
    }

    protected virtual void OnRemovedFromBody(EntityUid uid, SharedBodyPartComponent component, PartRemovedFromBodyEvent args)
    {
        if (!Deleted(uid))
        {
            Transform(uid).AttachToGridOrMap();
        }
    }

    protected virtual void OnInsertedIntoContainer(EntityUid uid, SharedBodyPartComponent component, EntInsertedIntoContainerMessage args)
    {
    }

    protected virtual void OnRemovedFromContainer(EntityUid uid, SharedBodyPartComponent component, EntRemovedFromContainerMessage args)
    {
    }
}
