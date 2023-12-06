using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using System;

namespace Content.Shared.Movement.Systems
{
    public abstract class ContactsSystem : EntitySystem
    {
        public override void Initialize_Contacts(Type ContactsComponentType)
        {
            base.Initialize();
            SubscribeLocalEvent<ContactsComponentType, StartCollideEvent>(OnEntityEnter);
            SubscribeLocalEvent<ContactsComponentType, EndCollideEvent>(OnEntityExit);
            SubscribeLocalEvent<ContactsComponentType, ComponentShutdown>(OnShutdown);
            if (ContactsComponentType == SlowContactsComponent)
                SubscribeLocalEvent<SlowedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
            UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        }

        private void OnEntityEnter_Contacts(EntityUid uid, Component component, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                return;

            if (typeof(component) == SlowContactsComponent)
                EnsureComp<SlowedByContactComponent>(otherUid);

            _toUpdate.Add(otherUid);
        }

        private void OnEntityExit_Contacts(EntityUid uid, Component component, ref EndCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (typeof(component) == FrictionContactsComponent)
            {
                if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                    return;
            }

            _toUpdate.Add(otherUid);
        }

        private void OnShutdown_Contacts(EntityUid uid, Component component, ComponentShutdown args)
        {
            if (!TryComp(uid, out PhysicsComponent? phys))
                return;

            // Note: For SlowContactsSystem, the entity may not be getting deleted here. E.g., glue puddles.
            _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
        }

        public override void Update_Contacts(float frameTime)
        {
            base.Update(frameTime);

            

            _toUpdate.Clear();
        }
    }
}
