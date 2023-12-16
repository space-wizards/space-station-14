using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Nutrition;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems
{
    public sealed partial class PolymorphSystem : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _compFact = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedBuckleSystem _buckle = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        private ISawmill _sawmill = default!;

        private const string RevertPolymorphId = "ActionRevertPolymorph";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
            SubscribeLocalEvent<PolymorphedEntityComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
            SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);
            SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);

            InitializeCollide();
            InitializeMap();

            _sawmill = Logger.GetSawmill("polymorph");
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

        private void OnRevertPolymorphActionEvent(EntityUid uid, PolymorphedEntityComponent component, RevertPolymorphActionEvent args)
        {
            Revert(uid, component);
        }

        private void OnMapInit(EntityUid uid, PolymorphedEntityComponent component, MapInitEvent args)
        {
            if (!_proto.TryIndex(component.Prototype, out PolymorphPrototype? proto))
            {
                // warning instead of error because of the all-comps one entity test.
                _sawmill.Warning($"{nameof(PolymorphSystem)} encountered an improperly set up polymorph component while initializing. Entity {ToPrettyString(uid)}. Prototype: {component.Prototype}");
                RemCompDeferred(uid, component);
                return;
            }

            if (proto.Forced)
                return;

            if (_actions.AddAction(uid, ref component.Action, out var action, RevertPolymorphId))
            {
                action.EntityIcon = component.Parent;
                action.UseDelay = TimeSpan.FromSeconds(proto.Delay);
            }
        }

        private void OnBeforeFullyEaten(EntityUid uid, PolymorphedEntityComponent comp, BeforeFullyEatenEvent args)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(comp.Prototype, out var proto))
            {
                _sawmill.Error($"Invalid polymorph prototype {comp.Prototype}");
                return;
            }

            if (proto.RevertOnEat)
            {
                args.Cancel();
                Revert(uid, comp);
            }
        }

        private void OnBeforeFullySliced(EntityUid uid, PolymorphedEntityComponent comp, BeforeFullySlicedEvent args)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(comp.Prototype, out var proto))
            {
                _sawmill.Error("Invalid polymorph prototype {comp.Prototype}");
                return;
            }

            if (proto.RevertOnEat)
            {
                args.Cancel();
                Revert(uid, comp);
            }
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
                _sawmill.Error("Invalid polymorph prototype {id}");
                return null;
            }

            return PolymorphEntity(target, proto);
        }

        /// <summary>
        /// Polymorphs the target entity into the specific polymorph prototype
        /// </summary>
        /// <param name="uid">The entity that will be transformed</param>
        /// <param name="proto">The polymorph prototype</param>
        public EntityUid? PolymorphEntity(EntityUid uid, PolymorphPrototype proto)
        {
            // if it's already morphed, don't allow it again with this condition active.
            if (!proto.AllowRepeatedMorphs && HasComp<PolymorphedEntityComponent>(uid))
                return null;

            // mostly just for vehicles
            _buckle.TryUnbuckle(uid, uid, true);

            var targetTransformComp = Transform(uid);

            var child = Spawn(proto.Entity, targetTransformComp.Coordinates);
            MakeSentientCommand.MakeSentient(child, EntityManager);

            var comp = _compFact.GetComponent<PolymorphedEntityComponent>();
            comp.Parent = uid;
            comp.Prototype = proto.ID;
            AddComp(child, comp);

            var childXform = Transform(child);
            childXform.LocalRotation = targetTransformComp.LocalRotation;

            if (_container.TryGetContainingContainer(uid, out var cont))
                cont.Insert(child);

            //Transfers all damage from the original to the new one
            if (proto.TransferDamage &&
                TryComp<DamageableComponent>(child, out var damageParent) &&
                _mobThreshold.GetScaledDamage(uid, child, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(child, damageParent, damage);
            }

            if (proto.Inventory == PolymorphInventoryChange.Transfer)
            {
                _inventory.TransferEntityInventories(uid, child);
                foreach (var hand in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, hand, checkActionBlocker: false);
                    _hands.TryPickupAnyHand(child, hand);
                }
            }
            else if (proto.Inventory == PolymorphInventoryChange.Drop)
            {
                if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
                {
                    while (enumerator.MoveNext(out var slot))
                    {
                        _inventory.TryUnequip(uid, slot.ID, true, true);
                    }
                }

                foreach (var held in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, held);
                }
            }

            if (proto.TransferName && TryComp<MetaDataComponent>(uid, out var targetMeta))
                _metaData.SetEntityName(child, targetMeta.EntityName);

            if (proto.TransferHumanoidAppearance)
            {
                _humanoid.CloneAppearance(uid, child);
            }

            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, child, mind: mind);

            //Ensures a map to banish the entity to
            EnsurePausesdMap();
            if (PausedMap != null)
                _transform.SetParent(uid, targetTransformComp, PausedMap.Value);

            return child;
        }

        /// <summary>
        /// Reverts a polymorphed entity back into its original form
        /// </summary>
        /// <param name="uid">The entityuid of the entity being reverted</param>
        /// <param name="component"></param>
        public EntityUid? Revert(EntityUid uid, PolymorphedEntityComponent? component = null)
        {
            if (Deleted(uid))
                return null;

            if (!Resolve(uid, ref component))
                return null;

            var parent = component.Parent;
            if (Deleted(parent))
                return null;

            if (!_proto.TryIndex(component.Prototype, out PolymorphPrototype? proto))
            {
                _sawmill.Error($"{nameof(PolymorphSystem)} encountered an improperly initialized polymorph component while reverting. Entity {ToPrettyString(uid)}. Prototype: {component.Prototype}");
                return null;
            }

            var uidXform = Transform(uid);
            var parentXform = Transform(parent);

            _transform.SetParent(parent, parentXform, uidXform.ParentUid);
            parentXform.Coordinates = uidXform.Coordinates;
            parentXform.LocalRotation = uidXform.LocalRotation;

            if (proto.TransferDamage &&
                TryComp<DamageableComponent>(parent, out var damageParent) &&
                _mobThreshold.GetScaledDamage(uid, parent, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(parent, damageParent, damage);
            }

            if (proto.Inventory == PolymorphInventoryChange.Transfer)
            {
                _inventory.TransferEntityInventories(uid, parent);
                foreach (var held in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, held);
                    _hands.TryPickupAnyHand(parent, held, checkActionBlocker: false);
                }
            }
            else if (proto.Inventory == PolymorphInventoryChange.Drop)
            {
                if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
                {
                    while (enumerator.MoveNext(out var slot))
                    {
                        _inventory.TryUnequip(uid, slot.ID);
                    }
                }

                foreach (var held in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, held);
                }
            }

            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, parent, mind: mind);

            // if an item polymorph was picked up, put it back down after reverting
            Transform(parent).AttachToGridOrMap();

            _popup.PopupEntity(Loc.GetString("polymorph-revert-popup-generic",
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(parent, EntityManager))),
                parent);
            QueueDel(uid);

            return parent;
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
                _sawmill.Error("Invalid polymorph prototype");
                return;
            }

            if (!TryComp<PolymorphableComponent>(target, out var polycomp))
                return;

            polycomp.PolymorphActions ??= new Dictionary<string, EntityUid>();
            if (polycomp.PolymorphActions.ContainsKey(id))
                return;

            var entproto = _proto.Index<EntityPrototype>(polyproto.Entity);

            EntityUid? actionId = default!;
            if (!_actions.AddAction(target, ref actionId, RevertPolymorphId, target))
                return;

            polycomp.PolymorphActions.Add(id, actionId.Value);
            _metaData.SetEntityName(actionId.Value, Loc.GetString("polymorph-self-action-name", ("target", entproto.Name)));
            _metaData.SetEntityDescription(actionId.Value, Loc.GetString("polymorph-self-action-description", ("target", entproto.Name)));

            if (!_actions.TryGetActionData(actionId, out var baseAction))
                return;

            baseAction.Icon = new SpriteSpecifier.EntityPrototype(polyproto.Entity);
            if (baseAction is InstantActionComponent action)
                action.Event = new PolymorphActionEvent { Prototype = polyproto };
        }

        [PublicAPI]
        public void RemovePolymorphAction(string id, EntityUid target, PolymorphableComponent? component = null)
        {
            if (!Resolve(target, ref component, false))
                return;
            if (component.PolymorphActions == null)
                return;
            if (component.PolymorphActions.TryGetValue(id, out var val))
                _actions.RemoveAction(target, val);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<PolymorphedEntityComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                comp.Time += frameTime;

                if (!_proto.TryIndex(comp.Prototype, out PolymorphPrototype? proto))
                {
                    _sawmill.Error($"{nameof(PolymorphSystem)} encountered an improperly initialized polymorph component while updating. Entity {ToPrettyString(uid)}. Prototype: {comp.Prototype}");
                    RemCompDeferred(uid, comp);
                    continue;
                }

                if (proto.Duration != null && comp.Time >= proto.Duration)
                {
                    Revert(uid, comp);
                    continue;
                }

                if (!TryComp<MobStateComponent>(uid, out var mob))
                    continue;

                if (proto.RevertOnDeath && _mobState.IsDead(uid, mob) ||
                    proto.RevertOnCrit && _mobState.IsIncapacitated(uid, mob))
                {
                    Revert(uid, comp);
                }
            }

            UpdateCollide();
        }
    }
}
