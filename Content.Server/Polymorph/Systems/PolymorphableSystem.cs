using Content.Server.Actions;
using Content.Server.Buckle.Systems;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems
{
    public sealed partial class PolymorphableSystem : EntitySystem
    {
        private readonly ISawmill _saw = default!;

        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly BuckleSystem _buckle = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IComponentFactory _compFact = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly ContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);

            InitializeCollide();
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
        public EntityUid? PolymorphEntity(EntityUid target, string id)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var proto))
            {
                _saw.Error("Invalid polymorph prototype");
                return null;
            }

            return PolymorphEntity(target, proto);
        }

        /// <summary>
        /// Polymorphs the target entity into the specific polymorph prototype
        /// </summary>
        /// <param name="target">The entity that will be transformed</param>
        /// <param name="proto">The polymorph prototype</param>
        public EntityUid? PolymorphEntity(EntityUid target, PolymorphPrototype proto)
        {
            // This is the big papa function. This handles the transformation, moving the old entity
            // logic and conditions specified in the prototype, and everything else that may be needed.
            // I am clinically insane - emo

            // if it's already morphed, don't allow it again with this condition active.
            if (!proto.AllowRepeatedMorphs && HasComp<PolymorphedEntityComponent>(target))
                return null;

            // mostly just for vehicles
            _buckle.TryUnbuckle(target, target, true);

            var targetTransformComp = Transform(target);

            var child = Spawn(proto.Entity, targetTransformComp.Coordinates);
            MakeSentientCommand.MakeSentient(child, EntityManager);

            var comp = _compFact.GetComponent<PolymorphedEntityComponent>();
            comp.Owner = child;
            comp.Parent = target;
            comp.Prototype = proto.ID;
            EntityManager.AddComponent(child, comp);

            var childXform = Transform(child);
            childXform.LocalRotation = targetTransformComp.LocalRotation;

            if (_container.TryGetContainingContainer(target, out var cont))
                cont.Insert(child);

            //Transfers all damage from the original to the new one
            if (proto.TransferDamage &&
                TryComp<DamageableComponent>(child, out var damageParent) &&
                _mobThresholdSystem.GetScaledDamage(target, child, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(damageParent, damage);
            }

            if (proto.Inventory == PolymorphInventoryChange.Transfer)
            {
                _inventory.TransferEntityInventories(target, child);
                foreach (var hand in _sharedHands.EnumerateHeld(target))
                {
                    hand.TryRemoveFromContainer();
                    _sharedHands.TryPickupAnyHand(child, hand);
                }
            }
            else if (proto.Inventory == PolymorphInventoryChange.Drop)
            {
                if(_inventory.TryGetContainerSlotEnumerator(target, out var enumerator))
                    while (enumerator.MoveNext(out var slot))
                        slot.EmptyContainer();

                foreach (var hand in _sharedHands.EnumerateHeld(target))
                    hand.TryRemoveFromContainer();
            }

            if (proto.TransferName &&
                TryComp<MetaDataComponent>(target, out var targetMeta) &&
                TryComp<MetaDataComponent>(child, out var childMeta))
            {
                childMeta.EntityName = targetMeta.EntityName;
            }

            if (proto.TransferHumanoidAppearance)
            {
                _humanoid.CloneAppearance(target, child);
            }

            if (TryComp<MindComponent>(target, out var mind) && mind.Mind != null)
                    mind.Mind.TransferTo(child);

            //Ensures a map to banish the entity to
            EnsurePausesdMap();
            if (PausedMap != null)
                targetTransformComp.AttachParent(Transform(PausedMap.Value));

            return child;
        }

        /// <summary>
        /// Creates a sidebar action for an entity to be able to polymorph at will
        /// </summary>
        /// <param name="id">The string of the id of the polymorph action</param>
        /// <param name="target">The entity that will be gaining the action</param>
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
                Event = new PolymorphActionEvent
                {
                    Prototype = polyproto,
                },
                DisplayName = Loc.GetString("polymorph-self-action-name", ("target", entproto.Name)),
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

    public sealed class PolymorphActionEvent : InstantActionEvent
    {
        /// <summary>
        /// The polymorph prototype containing all the information about
        /// the specific polymorph.
        /// </summary>
        public PolymorphPrototype Prototype = default!;
    };
}
