using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class FrictionContactsSystem : ContactsSystem
{
    public override void Initialize()
    {
        Initialize_Contacts(FrictionContactsComponent);
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        OnEntityEnter_Contacts(uid, component, args);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        OnEntityExit_Contacts(uid, component, args);
    }

    private void OnShutdown(EntityUid uid, FrictionContactsComponent component, ComponentShutdown args)
    {
        OnShutdown_Contacts(uid, component, args);
    }

    public override void Update(float frameTime, FrictionContactsComponent component)
    {
        Update_Contacts(frameTime, component);
    }

    private void ApplyFrictionChange(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        FrictionContactsComponent? frictionComponent = TouchesFrictionContactsComponent(uid, physicsComponent);

        if (frictionComponent == null)
        {
            _speedModifierSystem.ChangeFriction(uid, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
        }
        else
        {
            _speedModifierSystem.ChangeFriction(uid, frictionComponent.MobFriction, frictionComponent.MobFrictionNoInput, frictionComponent.MobAcceleration, speedModifier);
        }
    }

    private FrictionContactsComponent? TouchesFrictionContactsComponent(EntityUid uid, PhysicsComponent physicsComponent)
    {
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp(ent, out FrictionContactsComponent? frictionContacts))
                continue;

            return frictionContacts;
        }

        return null;
    }
}
