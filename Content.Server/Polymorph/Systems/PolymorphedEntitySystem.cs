using Content.Server.Actions;
using Content.Server.Inventory;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.MobState.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphedEntitySystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphedEntityComponent, PolymorphComponentSetupEvent>(OnInit);
            SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);
        }

        private void OnRevertPolymorphActionEvent(EntityUid uid, PolymorphedEntityComponent component, RevertPolymorphActionEvent args)
        {
            Revert(uid);
        }

        /// <summary>
        /// Reverts a polymorphed entity back into its original form
        /// </summary>
        /// <param name="uid">The entityuid of the entity being reverted</param>
        public void Revert(EntityUid uid)
        {
            if (!TryComp<PolymorphedEntityComponent>(uid, out var component))
                return;

            var proto = component.Prototype;

            var uidXform = Transform(uid);
            var parentXform = Transform(component.Parent);

            parentXform.AttachParent(uidXform.ParentUid);
            parentXform.Coordinates = uidXform.Coordinates;
            parentXform.LocalRotation = uidXform.LocalRotation;

            if (component.Prototype.TransferDamage &&
                TryComp<DamageableComponent>(component.Parent, out var damageParent) &&
                _damageable.GetScaledDamage(uid, component.Parent, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(damageParent, damage);
            }

            if (proto.DropInventory || proto.TransferInventory)
            {
                if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
                {
                    Dictionary<String, EntityUid?> inventoryEntities = new();
                    var slots = _inventory.GetSlots(uid);
                    while (enumerator.MoveNext(out var containerSlot))
                    {
                        //records all the entities stored in each of the target's slots
                        foreach (var slot in slots)
                        {
                            if (_inventory.TryGetSlotContainer(component.Parent, slot.Name, out var conslot, out var _) &&
                                conslot.ID == containerSlot.ID)
                            {
                                inventoryEntities.Add(slot.Name, containerSlot.ContainedEntity);
                            }
                        }
                        //drops everything in the target's inventory on the ground
                        containerSlot.EmptyContainer();
                    }

                    /// this is the specific code which takes the data about all the entities 
                    /// we stored earlier and actually equips all of it to the new entity
                    if (proto.TransferInventory)
                    {
                        foreach (var item in inventoryEntities)
                        {
                            if (item.Value != null)
                                _inventory.TryEquip(component.Parent, item.Value.Value, item.Key, true);
                        }
                    }
                }
                //drops everything in the user's hands
                foreach (var hand in _sharedHands.EnumerateHeld(uid))
                {
                    hand.TryRemoveFromContainer();
                    if (proto.TransferInventory)
                        _sharedHands.TryPickupAnyHand(component.Parent, hand);
                }
            }

            if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            {
                mind.Mind.TransferTo(component.Parent);
            }

            _popup.PopupEntity(Loc.GetString("polymorph-revert-popup-generic", ("parent", uid), ("child", component.Parent)), component.Parent, Filter.Pvs(component.Parent));
            QueueDel(uid);
        }

        private void OnInit(EntityUid uid, PolymorphedEntityComponent component, PolymorphComponentSetupEvent args)
        {
            if (component.Prototype.Forced)
                return;

            var act = new InstantAction()
            {
                Event = new RevertPolymorphActionEvent(),
                EntityIcon = component.Parent,
                Name = Loc.GetString("polymorph-revert-action-name"),
                Description = Loc.GetString("polymorph-revert-action-description"),
                UseDelay = TimeSpan.FromSeconds(component.Prototype.Delay),
           };
    
            _actions.AddAction(uid, act, null);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in EntityQuery<PolymorphedEntityComponent>())
            {
                entity.Time += frameTime;

                if(entity.Prototype.Duration != null && entity.Time >= entity.Prototype.Duration)
                    Revert(entity.Owner);

                if (!TryComp<MobStateComponent>(entity.Owner, out var mob))
                    continue;

                if ((entity.Prototype.RevertOnDeath && mob.IsDead()) ||
                    (entity.Prototype.RevertOnCrit && mob.IsCritical()))
                    Revert(entity.Owner);
            }
        }
    }

    public sealed class RevertPolymorphActionEvent : InstantActionEvent { };
}
