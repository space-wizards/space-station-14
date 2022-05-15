using Content.Server.Actions;
using Content.Server.Buckle.Components;
using Content.Server.Hands.Components;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Polymorph;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphableSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnStartup); //remove this
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
        }

        // most of this is placeholder stuff meant for testing. make sure this is gone
        private void OnStartup(EntityUid uid, PolymorphableComponent component, ComponentStartup args)
        {
            CreatePolymorphAction("mouse", component.Owner); //remove
            CreatePolymorphAction("chickenForced", component.Owner);

            if (component.InnatePolymorphs != null)
            {
                foreach (var morph in component.InnatePolymorphs)
                {
                    CreatePolymorphAction(morph, uid);
                }
            }
        }

        private void OnPolymorphActionEvent(EntityUid uid, PolymorphableComponent component, PolymorphActionEvent args)
        {
            PolymorphEntity(uid, args.Prototype);
        }

        public void PolymorphEntity(EntityUid target, String id)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var prototype))
                return;

            PolymorphEntity(target, prototype);
        }

        public void PolymorphEntity(EntityUid target, PolymorphPrototype proto)
        {
            if (!TryComp<MindComponent>(target, out var mind))
                return;

            if (mind.Mind == null)
                return;

            // mostly just for vehicles
            if (TryComp<BuckleComponent>(target, out var buckle))
                buckle.TryUnbuckle(target, true);

            var child = Spawn(proto.Entity, Transform(target).Coordinates);
            MakeSentientCommand.MakeSentient(child, _entity);
            var comp = AddComp<PolymorphedEntityComponent>(child);
            comp.Parent = target;
            comp.Prototype = proto;

            if (proto.DropInventory)
            {
                if (_inventory.TryGetContainerSlotEnumerator(target, out var enumerator))
                {
                    while (enumerator.MoveNext(out var containerSlot))
                    {
                        containerSlot.EmptyContainer();
                    }
                }
                foreach (var hand in _sharedHands.EnumerateHeld(target))
                {
                    hand.TryRemoveFromContainer();
                }
            }

            //TODO: remove the container system altogether
            comp.ParentContainer.Insert(target);
            RaiseLocalEvent(child, new AfterPolymorphEvent());
            mind.Mind.TransferTo(child);
        }

        public void CreatePolymorphAction(string id, EntityUid target)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var polyproto))
                return;

            if (!TryComp<PolymorphableComponent>(target, out var comp))
                return;

            if (!_proto.TryIndex<EntityPrototype>(polyproto.Entity, out var entproto))
                return;

            var act = new InstantAction()
            {
                Event = new PolymorphActionEvent(polyproto),
                Name = Loc.GetString("polymorph-self-action-name", ("target", entproto.Name)),
                Description = Loc.GetString("polymorph-self-action-description", ("target", entproto.Name)),
            };

            comp.PolymorphActions.Add(id, act);
            _actions.AddAction(target, act, target);
        }

        public void RemovePolymorphAction(string id, EntityUid target)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var polyproto))
                return;

            if (!TryComp<PolymorphableComponent>(target, out var comp))
                return;

            foreach (var action in comp.PolymorphActions)
            {
                if(action.Key == id)
                {
                    _actions.RemoveAction(target, action.Value);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// This event is used to initialize the info in polymorphedEntityComponent
    /// once all the information has been sent to it.
    /// </summary>
    public sealed class AfterPolymorphEvent : EventArgs { }

    public sealed class PolymorphActionEvent : InstantActionEvent
    {
        /// <summary>
        /// The polymorph prototype containing all the information about
        /// the specific polymorph.
        /// </summary>
        public readonly PolymorphPrototype Prototype = new();

        public PolymorphActionEvent(PolymorphPrototype prototype)
        {
            Prototype = prototype;
        }
    };
}
