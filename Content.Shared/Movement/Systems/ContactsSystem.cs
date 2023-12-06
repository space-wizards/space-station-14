using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems
{
    public class ContactsSystem : EntitySystem
    {
        [Dependency] protected readonly SharedPhysicsSystem _physics = default!;
        [Dependency] protected readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

        // Comment copied from "original" SlowContactsSystem.cs
        // TODO full-game-save
        // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
        protected HashSet<EntityUid> _toUpdate = new();
        protected HashSet<EntityUid> _toRemove = new();
        public void Initialize()
        {
            base.Initialize();
        }

        protected void OnEntityEnter_Contacts(EntityUid uid, Component component, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                return;

            if (typeof(SlowContactsComponent) == component.GetType())
                EnsureComp<SlowedByContactComponent>(otherUid);

            _toUpdate.Add(otherUid);
        }

        protected void OnEntityExit_Contacts(EntityUid uid, Component component, ref EndCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (typeof(FrictionContactsComponent) == component.GetType())
            {
                if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
                    return;
            }

            _toUpdate.Add(otherUid);
        }

        protected void OnShutdown_Contacts(EntityUid uid, Component component, ComponentShutdown args)
        {
            if (!TryComp(uid, out PhysicsComponent? phys))
                return;

            // Note: For SlowContactsSystem, the entity may not be getting deleted here. E.g., glue puddles.
            _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
        }

        protected void Update_Contacts(float frameTime, Component component)
        {
            base.Update(frameTime);

            if (typeof(FrictionContactsComponent) == component.GetType())
            {
                foreach (var uid in _toUpdate)
                {

                    FrictionContactsSystem frictionContactsSystem = new FrictionContactsSystem();
                    frictionContactsSystem.ApplyFrictionChange(uid);
                }
            }

            if (typeof(SlowContactsComponent) == component.GetType())
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
