using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Server.Mind.Components;
using Content.Shared.Tag;

namespace Content.Server.Borgs
{
    public sealed class InnateItemSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InnateItemComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<InnateItemComponent, InnateAfterInteractActionEvent>(StartAfterInteract);
        }

        private void OnMindAdded(EntityUid uid, InnateItemComponent component, MindAddedMessage args)
        {
            if (!component.AlreadyInitialized)
                RefreshItems(uid);

            component.AlreadyInitialized = true;
        }

        private void RefreshItems(EntityUid uid)
        {
            if (!TryComp<ItemSlotsComponent>(uid, out var slotsComp))
                return;
            foreach (var slot in slotsComp.Slots.Values)
            {

                if (slot.ContainerSlot == null)
                    continue;
                var sourceItem = slot.ContainerSlot.ContainedEntity;
                if (sourceItem == null)
                    continue;
                if (_tagSystem.HasTag((EntityUid) sourceItem, "NoAction"))
                    continue;

                _actionsSystem.AddAction(uid, CreateAction((EntityUid) sourceItem), uid);
            }
        }

        private EntityTargetAction CreateAction(EntityUid uid)
        {
            EntityTargetAction action = new()
            {
                Name = MetaData(uid).EntityName,
                Description = MetaData(uid).EntityDescription,
                EntityIcon = uid,
                Event = new InnateAfterInteractActionEvent(uid),
            };

            return action;
        }
        private void StartAfterInteract(EntityUid uid, InnateItemComponent component, InnateAfterInteractActionEvent args)
        {
            var ev = new AfterInteractEvent(args.Performer, args.Item, args.Target, Transform(args.Target).Coordinates, true);
            RaiseLocalEvent(args.Item, ev, false);
        }
    }

    public sealed class InnateAfterInteractActionEvent : EntityTargetActionEvent
    {
        public EntityUid Item;

        public InnateAfterInteractActionEvent(EntityUid item)
        {
            Item = item;
        }
    }
}
