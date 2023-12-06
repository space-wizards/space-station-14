using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems
{
    public abstract class ContactsSystem : EntitySystem
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

        // Comment copied from "original" SlowContactsSystem.cs
        // TODO full-game-save
        // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
        private HashSet<EntityUid> _toUpdate = new();
        private HashSet<EntityUid> _toRemove = new();
        public abstract void Initialize_Contacts(Type ContactsComponentType)
        {
            base.Initialize();
            SubscribeLocalEvent<ContactsComponentType, StartCollideEvent>(OnEntityEnter);
            SubscribeLocalEvent<ContactsComponentType, EndCollideEvent>(OnEntityExit);
            SubscribeLocalEvent<ContactsComponentType, ComponentShutdown>(OnShutdown);
            if (ContactsComponentType == SlowContactsComponent)
                SubscribeLocalEvent<SlowedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
            UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        }

        private abstract void OnEntityEnter_Contacts(EntityUid uid, Component component, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                return;

            if (typeof(component) == SlowContactsComponent)
                EnsureComp<SlowedByContactComponent>(otherUid);

            _toUpdate.Add(otherUid);
        }

        private abstract void OnEntityExit_Contacts(EntityUid uid, Component component, ref EndCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (typeof(component) == FrictionContactsComponent)
            {
                if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                    return;
            }

            _toUpdate.Add(otherUid);
        }

        private abstract void OnShutdown_Contacts(EntityUid uid, Component component, ComponentShutdown args)
        {
            if (!TryComp(uid, out PhysicsComponent? phys))
                return;

            // Note: For SlowContactsSystem, the entity may not be getting deleted here. E.g., glue puddles.
            _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
        }

        public abstract void Update_Contacts(float frameTime, Component component)
        {
            base.Update(frameTime);

            if (typeof(component) == FrictionContactsComponent)
            {
                foreach (var uid in _toUpdate)
                {
                    ApplyFrictionChange(uid);
                }
            }

            if (typeof(component) == SlowContactsComponent)
            {
                foreach (var ent in _toUpdate)
                {
                    _speedModifierSystem.RefreshMovementSpeedModifiers(ent);
                }

                foreach (var ent in _toRemove)
                {
                    RemComp<SlowedByContactComponent>(ent);
                }
            }

            _toUpdate.Clear();
        }
    }
}
