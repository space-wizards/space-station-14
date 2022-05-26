using Content.Server.Actions;
using Content.Server.Buckle.Components;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Polymorph;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems
{
    public sealed partial class PolymorphableSystem : EntitySystem
    {
        private readonly ISawmill _saw = default!;

        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);

            InitializeMap();
        }

        private void OnStartup(EntityUid uid, PolymorphableComponent component, ComponentStartup args)
        {
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

        /// <summary>
        /// Polymorphs the target entity into the specific polymorph prototype
        /// </summary>
        /// <param name="target">The entity that will be transformed</param>
        /// <param name="id">The id of the polymorph prototype</param>
        public void PolymorphEntity(EntityUid target, String id)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var proto))
            {
                _saw.Error("Invalid polymorph prototype");
                return;
            }

            PolymorphEntity(target, proto);
        }

        /// <summary>
        /// Polymorphs the target entity into the specific polymorph prototype
        /// </summary>
        /// <param name="target">The entity that will be transformed</param>
        /// <param name="proto">The polymorph prototype</param>
        public void PolymorphEntity(EntityUid target, PolymorphPrototype proto)
        {
            // mostly just for vehicles
            if (TryComp<BuckleComponent>(target, out var buckle))
                buckle.TryUnbuckle(target, true);

            var targetTransformComp = Transform(target);

            var child = Spawn(proto.Entity, targetTransformComp.Coordinates);
            MakeSentientCommand.MakeSentient(child, EntityManager);

            var comp = EnsureComp<PolymorphedEntityComponent>(child);
            comp.Parent = target;
            comp.Prototype = proto;
            RaiseLocalEvent(child, new PolymorphComponentSetupEvent());

            //Transfers all damage from the original to the new one
            if (TryComp<DamageableComponent>(child, out var damageParent) &&
                _damageable.GetScaledDamage(target, child, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(damageParent, damage);
            }

            if (proto.DropInventory)
            {
                //drops everything in the user's inventory
                if (_inventory.TryGetContainerSlotEnumerator(target, out var enumerator))
                {
                    while (enumerator.MoveNext(out var containerSlot))
                    {
                        containerSlot.EmptyContainer();
                    }
                }
                //drops everything in the user's hands
                foreach (var hand in _sharedHands.EnumerateHeld(target))
                {
                    hand.TryRemoveFromContainer();
                }
            }

            if (TryComp<MindComponent>(target, out var mind) && mind.Mind != null)
                mind.Mind.TransferTo(child);

            //Ensures a map to banish the entity to
            EnsurePausesdMap();
            if(PausedMap != null)
                targetTransformComp.AttachParent(Transform(PausedMap.Value));
            
            _popup.PopupEntity(Loc.GetString("polymorph-popup-generic", ("parent", target), ("child", child)), child, Filter.Pvs(child));
        }

        public void CreatePolymorphAction(string id, EntityUid target)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var polyproto))
            {
                _saw.Error("Invalid polymorph prototype");
                return;
            }
                
            if (!TryComp<PolymorphableComponent>(target, out var polycomp))
                return;

            var entproto = _proto.Index<EntityPrototype>(polyproto.Entity);

            var act = new InstantAction()
            {
                Event = new PolymorphActionEvent(polyproto),
                Name = Loc.GetString("polymorph-self-action-name", ("target", entproto.Name)),
                Description = Loc.GetString("polymorph-self-action-description", ("target", entproto.Name)),
                Icon = new SpriteSpecifier.EntityPrototype(polyproto.Entity),
                ItemIconStyle = ItemActionIconStyle.NoItem,
            };

            if (polycomp.PolymorphActions == null)
                polycomp.PolymorphActions = new();

            polycomp.PolymorphActions.Add(id, act);
            _actions.AddAction(target, act, target);
        }

        public void RemovePolymorphAction(string id, EntityUid target)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var polyproto))
                return;
            if (!TryComp<PolymorphableComponent>(target, out var comp))
                return;
            if (comp.PolymorphActions == null)
                return;

            comp.PolymorphActions.TryGetValue(id, out var val);
            if (val != null)
                _actions.RemoveAction(target, val);
        }
    }

    /// <summary>
    /// Used after the polymorphedEntity component has it's data set up.
    /// </summary>
    public sealed class PolymorphComponentSetupEvent : InstantActionEvent { };

    public sealed class PolymorphActionEvent : InstantActionEvent
    {
        /// <summary>
        /// The polymorph prototype containing all the information about
        /// the specific polymorph.
        /// </summary>
        public readonly PolymorphPrototype Prototype;

        public PolymorphActionEvent(PolymorphPrototype prototype)
        {
            Prototype = prototype;
        }
    };
}
