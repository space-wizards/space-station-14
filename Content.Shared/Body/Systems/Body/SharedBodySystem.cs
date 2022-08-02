using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems.Body
{
    public abstract partial class SharedBodySystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
        [Dependency] protected readonly SharedBodyPartSystem BodyPartSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedBodyComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedBodyComponent, ComponentGetState>(OnComponentGetState);
            SubscribeLocalEvent<SharedBodyComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
            SubscribeLocalEvent<SharedBodyComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        }

        protected virtual void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
        {
            UpdateFromTemplate(uid, component.TemplateId, component);
        }

        public void OnComponentGetState(EntityUid uid, SharedBodyComponent body, ref ComponentGetState args)
        {
            args.State = new BodyComponentState(body.Slots);
        }

        protected virtual void OnInsertedIntoContainer(EntityUid uid, SharedBodyComponent body, EntInsertedIntoContainerMessage args)
        {
            if (!TryComp<SharedBodyPartComponent>(args.Entity, out var part))
                return;

            var ev = new PartAddedToBodyEvent(uid, part.Owner, args.Container.ID);
            RaiseLocalEvent(uid, ev);
            RaiseLocalEvent(part.Owner, ev);
        }

        protected virtual void OnRemovedFromContainer(EntityUid uid, SharedBodyComponent body, EntRemovedFromContainerMessage args)
        {
            if (!TryComp<SharedBodyPartComponent>(args.Entity, out var part))
                return;

            var ev = new PartRemovedFromBodyEvent(uid, args.Entity, args.Container.ID);
            RaiseLocalEvent(uid, ev);
            RaiseLocalEvent(args.Entity, ev);
        }
    }
}
