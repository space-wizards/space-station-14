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
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedBodyPartComponent component, ComponentInit args)
    {
        component.MechanismContainer =
            ContainerSystem.EnsureContainer<Container>(uid, ContainerName);
    }

    protected virtual void OnComponentGetState(EntityUid uid, SharedBodyPartComponent component, ref ComponentGetState args)
    {
        args.State = new BodyPartComponentState(component.PartType, component.Symmetry);
    }

    protected virtual void OnComponentHandleState(EntityUid uid, SharedBodyPartComponent component, ref ComponentHandleState args)
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

        if (TryComp<SharedBodyComponent>(args.Body, out var body))
            component.Body = body;

        if (!ContainerSystem.TryGetContainer(uid, ContainerName, out var mechanismContainer))
            return;

        foreach (var ent in mechanismContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, new MechanismAddedToBodyEvent(args.Body), true);
        }
    }

    protected virtual void OnRemovedFromBody(EntityUid uid, SharedBodyPartComponent component, PartRemovedFromBodyEvent args)
    {
        if (!Deleted(uid))
        {
            Transform(uid).AttachToGridOrMap();
        }

        component.Body = null;

        if (!ContainerSystem.TryGetContainer(uid, ContainerName, out var mechanismContainer))
            return;

        foreach (var ent in mechanismContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, new MechanismRemovedFromBodyEvent(args.Body), true);
        }
    }
}
