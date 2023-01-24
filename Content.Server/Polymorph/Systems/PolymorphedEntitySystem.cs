using Content.Server.Actions;
using Content.Server.Inventory;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphedEntitySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphedEntityComponent, ComponentStartup>(OnInit);
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
            if (Deleted(uid))
                return;

            if (!TryComp<PolymorphedEntityComponent>(uid, out var component))
                return;

            if (Deleted(component.Parent))
                return;

            if (!_proto.TryIndex(component.Prototype, out PolymorphPrototype? proto))
            {
                Logger.Error($"{nameof(PolymorphedEntitySystem)} encountered an improperly initialized polymorph component while reverting. Entity {ToPrettyString(uid)}. Prototype: {component.Prototype}");
                return;
            }

            var uidXform = Transform(uid);
            var parentXform = Transform(component.Parent);

            parentXform.AttachParent(uidXform.ParentUid);
            parentXform.Coordinates = uidXform.Coordinates;
            parentXform.LocalRotation = uidXform.LocalRotation;

            if (_container.TryGetContainingContainer(uid, out var cont))
                cont.Insert(component.Parent);

            if (proto.TransferDamage &&
                TryComp<DamageableComponent>(component.Parent, out var damageParent) &&
                _mobThresholdSystem.GetScaledDamage(uid, component.Parent, out var damage) &&
                damage != null)
            {
                _damageable.SetDamage(damageParent, damage);
            }

            if (proto.Inventory == PolymorphInventoryChange.Transfer)
            {
                _inventory.TransferEntityInventories(uid, component.Parent);
                foreach (var hand in _sharedHands.EnumerateHeld(component.Parent))
                {
                    hand.TryRemoveFromContainer();
                    _sharedHands.TryPickupAnyHand(component.Parent, hand);
                }
            }
            else if (proto.Inventory == PolymorphInventoryChange.Drop)
            {
                if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
                    while (enumerator.MoveNext(out var slot))
                        slot.EmptyContainer();

                foreach (var hand in _sharedHands.EnumerateHeld(uid))
                    // This causes errors/bugs. Use hand related functions instead.
                    hand.TryRemoveFromContainer();
            }

            if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            {
                mind.Mind.TransferTo(component.Parent);
            }

            _popup.PopupEntity(Loc.GetString("polymorph-revert-popup-generic",
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(component.Parent, EntityManager))),
                component.Parent);
            QueueDel(uid);
        }

        public void OnInit(EntityUid uid, PolymorphedEntityComponent component, ComponentStartup args)
        {
            if (!_proto.TryIndex(component.Prototype, out PolymorphPrototype? proto))
            {
                // warning instead of error because of the all-comps one entity test.
                Logger.Warning($"{nameof(PolymorphedEntitySystem)} encountered an improperly set up polymorph component while initializing. Entity {ToPrettyString(uid)}. Prototype: {component.Prototype}");
                RemCompDeferred(uid, component);
                return;
            }

            if (proto.Forced)
                return;

            var act = new InstantAction()
            {
                Event = new RevertPolymorphActionEvent(),
                EntityIcon = component.Parent,
                DisplayName = Loc.GetString("polymorph-revert-action-name"),
                Description = Loc.GetString("polymorph-revert-action-description"),
                UseDelay = TimeSpan.FromSeconds(proto.Delay),
           };

            _actions.AddAction(uid, act, null);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<PolymorphedEntityComponent>())
            {
                comp.Time += frameTime;

                if (!_proto.TryIndex(comp.Prototype, out PolymorphPrototype? proto))
                {
                    Logger.Error($"{nameof(PolymorphedEntitySystem)} encountered an improperly initialized polymorph component while updating. Entity {ToPrettyString(comp.Owner)}. Prototype: {comp.Prototype}");
                    RemCompDeferred(comp.Owner, comp);
                    continue;
                }

                if(proto.Duration != null && comp.Time >= proto.Duration)
                    Revert(comp.Owner);

                if (!TryComp<MobStateComponent>(comp.Owner, out var mob))
                    continue;

                if ((proto.RevertOnDeath && _mobStateSystem.IsDead(comp.Owner, mob)) ||
                    (proto.RevertOnCrit && _mobStateSystem.IsCritical(comp.Owner, mob)))
                    Revert(comp.Owner);
            }
        }
    }

    public sealed class RevertPolymorphActionEvent : InstantActionEvent { };
}
