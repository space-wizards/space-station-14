using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Movement.Systems;

public sealed class FrictionContactsSystem : SharedContactsSystem<FrictionContactsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrictionContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FrictionContactsComponent, EndCollideEvent>(OnEntityExit);
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
            return;

        EntityUpdateContacts(otherUid);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
            return;

        EntityUpdateContacts(otherUid);
    }

    protected override void OnUpdate(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        var frictionComponent = TouchesFrictionContactsComponent(uid, physicsComponent);

        if (frictionComponent == null)
        {
            SpeedModifierSystem.ChangeFriction(uid, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
        }
        else
        {
            SpeedModifierSystem.ChangeFriction(uid, frictionComponent.MobFriction, frictionComponent.MobFrictionNoInput, frictionComponent.MobAcceleration, speedModifier);
        }
    }

    private FrictionContactsComponent? TouchesFrictionContactsComponent(EntityUid uid, PhysicsComponent physicsComponent)
    {
        foreach (var ent in Physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp(ent, out FrictionContactsComponent? frictionContacts))
                continue;

            return frictionContacts;
        }

        return null;
    }
}
