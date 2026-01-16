using Content.Shared.DragDrop;
using Robust.Shared.Containers;

namespace Content.Shared.Body;

public sealed partial class BodySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private EntityQuery<Body.BodyComponent> _bodyQuery;
    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Body.BodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<Body.BodyComponent, ComponentShutdown>(OnBodyShutdown);

        SubscribeLocalEvent<Body.BodyComponent, CanDragEvent>(OnCanDrag);

        SubscribeLocalEvent<Body.BodyComponent, EntInsertedIntoContainerMessage>(OnBodyEntInserted);
        SubscribeLocalEvent<Body.BodyComponent, EntRemovedFromContainerMessage>(OnBodyEntRemoved);

        _bodyQuery = GetEntityQuery<Body.BodyComponent>();
        _organQuery = GetEntityQuery<OrganComponent>();

        InitializeRelay();
    }

    private void OnBodyInit(Entity<Body.BodyComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Organs =
            _container.EnsureContainer<Container>(ent, Body.BodyComponent.ContainerID);
    }

    private void OnBodyShutdown(Entity<Body.BodyComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Organs is { } organs)
            _container.ShutdownContainer(organs);
    }

    private void OnBodyEntInserted(Entity<Body.BodyComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != Body.BodyComponent.ContainerID)
            return;

        if (!_organQuery.TryComp(args.Entity, out var organ))
            return;

        var body = new OrganInsertedIntoEvent(args.Entity);
        RaiseLocalEvent(ent, ref body);

        var ev = new OrganGotInsertedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        if (organ.Body != ent)
        {
            organ.Body = ent;
            Dirty(args.Entity, organ);
        }
    }

    private void OnBodyEntRemoved(Entity<Body.BodyComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != Body.BodyComponent.ContainerID)
            return;

        if (!_organQuery.TryComp(args.Entity, out var organ))
            return;

        var body = new OrganRemovedFromEvent(args.Entity);
        RaiseLocalEvent(ent, ref body);

        var ev = new OrganGotRemovedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        if (organ.Body == null)
            return;

        organ.Body = null;
        Dirty(args.Entity, organ);
    }

    private void OnCanDrag(Entity<Body.BodyComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }
}
