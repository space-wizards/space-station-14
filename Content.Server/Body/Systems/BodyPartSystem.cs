using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Containers;

namespace Content.Server.Body.Systems;

public sealed class BodyPartSystem : SharedBodyPartSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBodyPartComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SharedBodyPartComponent component, MapInitEvent args)
    {
        foreach (var mechanismId in component.InitialMechanisms)
        {
            var entity = Spawn(mechanismId, Transform(uid).Coordinates);

            if (!TryComp<MechanismComponent>(entity, out var mechanism))
            {
                Logger.Error($"Entity {mechanismId} does not have a {nameof(MechanismComponent)} component.");
                continue;
            }

            TryAddMechanism(uid, mechanism, true, component);
        }
    }

    protected override void OnAddedToBody(EntityUid uid, SharedBodyPartComponent component, PartAddedToBodyEvent args)
    {
        base.OnAddedToBody(uid, component, args);

        if (TryComp<SharedBodyComponent>(args.Body, out var body))
            component.Body = body;

        if (!ContainerSystem.TryGetContainer(uid, ContainerName, out var mechanismContainer))
            return;

        foreach (var ent in mechanismContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, new MechanismAddedToBodyEvent(args.Body), true);
        }
    }

    protected override void OnRemovedFromBody(EntityUid uid, SharedBodyPartComponent component, PartRemovedFromBodyEvent args)
    {
        base.OnRemovedFromBody(uid, component, args);

        component.Body = null;

        if (!ContainerSystem.TryGetContainer(uid, ContainerName, out var mechanismContainer))
            return;

        foreach (var ent in mechanismContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, new MechanismRemovedFromBodyEvent(args.Body), true);
        }
    }

    protected override void OnInsertedIntoContainer(EntityUid uid, SharedBodyPartComponent component, EntInsertedIntoContainerMessage args)
    {
        base.OnInsertedIntoContainer(uid, component, args);

        if (!TryComp<MechanismComponent>(args.Entity, out var mechanism))
            return;

        mechanism.Part = component;

        if (component.Body == null)
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToPartEvent(component.Owner));
        else
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToPartInBodyEvent(component.Body.Owner, component.Owner));
    }

    protected override void OnRemovedFromContainer(EntityUid uid, SharedBodyPartComponent component, EntRemovedFromContainerMessage args)
    {
        base.OnRemovedFromContainer(uid, component, args);

        if (!TryComp<MechanismComponent>(args.Entity, out var mechanism))
            return;

        mechanism.Part = null;

        if (component.Body == null)
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromPartEvent(component.Owner));
        else
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromPartInBodyEvent(component.Body.Owner, component.Owner));
    }

    /// <summary>
    ///     Gibs the body part.
    /// </summary>
    public HashSet<EntityUid> Gib(EntityUid uid, SharedBodyPartComponent? part = null)
    {
        var gibs = new HashSet<EntityUid>();
        if (!Resolve(uid, ref part))
            return gibs;

        foreach (var mechanism in GetAllMechanisms(uid, part))
        {
            gibs.Add(part.Owner);
            TryRemoveMechanism(uid, mechanism, part);
        }

        return gibs;
    }
}
