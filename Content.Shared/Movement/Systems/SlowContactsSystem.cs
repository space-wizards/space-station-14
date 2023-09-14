using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class SlowContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    private HashSet<EntityUid> _toUpdate = new();
    private HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlowContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SlowContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<SlowedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
        SubscribeLocalEvent<SlowContactsComponent, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _toRemove.Clear();

        foreach (var ent in _toUpdate)
        {
            _speedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        foreach (var ent in _toRemove)
        {
            RemComp<SlowedByContactComponent>(ent);
        }

        _toUpdate.Clear();
    }

    public void ChangeModifiers(EntityUid uid, float speed, SlowContactsComponent? component = null)
    {
        ChangeModifiers(uid, speed, speed, component);
    }

    public void ChangeModifiers(EntityUid uid, float walkSpeed, float sprintSpeed, SlowContactsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }
        component.WalkSpeedModifier = walkSpeed;
        component.SprintSpeedModifier = sprintSpeed;
        Dirty(component);
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid));
    }

    private void OnShutdown(EntityUid uid, SlowContactsComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        // Note that the entity may not be getting deleted here. E.g., glue puddles.
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
    }

    private void MovementSpeedCheck(EntityUid uid, SlowedByContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        var walkSpeed = 1.0f;
        var sprintSpeed = 1.0f;

        bool remove = true;
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp<SlowContactsComponent>(ent, out var slowContactsComponent))
                continue;

            if (slowContactsComponent.IgnoreWhitelist != null && slowContactsComponent.IgnoreWhitelist.IsValid(uid))
                continue;

            walkSpeed = Math.Min(walkSpeed, slowContactsComponent.WalkSpeedModifier);
            sprintSpeed = Math.Min(sprintSpeed, slowContactsComponent.SprintSpeedModifier);
            remove = false;
        }

        args.ModifySpeed(walkSpeed, sprintSpeed);

        // no longer colliding with anything
        if (remove)
            _toRemove.Add(uid);
    }

    private void OnEntityExit(EntityUid uid, SlowContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        _toUpdate.Add(otherUid);
    }

    private void OnEntityEnter(EntityUid uid, SlowContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (!HasComp<MovementSpeedModifierComponent>(otherUid))
            return;

        EnsureComp<SlowedByContactComponent>(otherUid);
        _toUpdate.Add(otherUid);
    }
}
