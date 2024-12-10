using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Slippery;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Movement.Systems;

public sealed class SpeedModifierContactsSystem : SharedContactsSystem<SpeedModifierContactsComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedModifierContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SpeedModifierContactsComponent, EndCollideEvent>(OnEntityExit);

        SubscribeLocalEvent<SpeedModifiedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
    }

    private void OnEntityEnter(EntityUid uid, SpeedModifierContactsComponent component, ref StartCollideEvent args)
    {
        AddModifiedEntity(args.OtherEntity);
    }

    private void OnEntityExit(EntityUid uid, SpeedModifierContactsComponent component, ref EndCollideEvent args)
    {
        EntityUpdateContacts(args.OtherEntity);
    }

    private void MovementSpeedCheck(EntityUid uid, SpeedModifiedByContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        var walkSpeed = 0.0f;
        var sprintSpeed = 0.0f;

        var remove = true;
        var entries = 0;

        foreach (var ent in Physics.GetContactingEntities(uid, physicsComponent))
        {
            var speedModified = false;

            if (TryComp<SpeedModifierContactsComponent>(ent, out var slowContactsComponent))
            {
                if (_whitelistSystem.IsWhitelistPass(slowContactsComponent.IgnoreWhitelist, uid))
                    continue;

                walkSpeed += slowContactsComponent.WalkSpeedModifier;
                sprintSpeed += slowContactsComponent.SprintSpeedModifier;
                speedModified = true;
            }

            // SpeedModifierContactsComponent takes priority over SlowedOverSlipperyComponent, effectively overriding the slippery slow.
            if (TryComp<SlipperyComponent>(ent, out var slipperyComponent) && speedModified == false)
            {
                var evSlippery = new GetSlowedOverSlipperyModifierEvent();
                RaiseLocalEvent(uid, ref evSlippery);

                if (evSlippery.SlowdownModifier != 1)
                {
                    walkSpeed += evSlippery.SlowdownModifier;
                    sprintSpeed += evSlippery.SlowdownModifier;
                    speedModified = true;
                }
            }

            if (speedModified)
            {
                remove = false;
                entries++;
            }
        }

        if (entries > 0)
        {
            walkSpeed /= entries;
            sprintSpeed /= entries;

            var evMax = new GetSpeedModifierContactCapEvent();
            RaiseLocalEvent(uid, ref evMax);

            walkSpeed = MathF.Max(walkSpeed, evMax.MaxWalkSlowdown);
            sprintSpeed = MathF.Max(sprintSpeed, evMax.MaxSprintSlowdown);

            args.ModifySpeed(walkSpeed, sprintSpeed);
        }

        // no longer colliding with anything
        if (remove)
            ToRemove.Add(uid);
    }

    protected override void OnUpdate(EntityUid uid)
    {
        SpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    public void ChangeModifiers(EntityUid uid, float speed, SpeedModifierContactsComponent? component = null)
    {
        ChangeModifiers(uid, speed, speed, component);
    }

    public void ChangeModifiers(EntityUid uid, float walkSpeed, float sprintSpeed, SpeedModifierContactsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        component.WalkSpeedModifier = walkSpeed;
        component.SprintSpeedModifier = sprintSpeed;

        Dirty(uid, component);

        ToUpdate.UnionWith(Physics.GetContactingEntities(uid));
    }

    /// <summary>
    /// Add an entity to be checked for speed modification from contact with another entity.
    /// </summary>
    /// <param name="uid">The entity to be added.</param>
    public void AddModifiedEntity(EntityUid uid)
    {
        if (!HasComp<MovementSpeedModifierComponent>(uid))
            return;

        EnsureComp<SpeedModifiedByContactComponent>(uid);

        EntityUpdateContacts(uid);
    }
}
