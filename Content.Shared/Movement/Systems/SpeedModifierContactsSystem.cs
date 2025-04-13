using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components; // imp edit
using Content.Shared.StepTrigger.Systems; // imp edit
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components; // imp edit
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class SpeedModifierContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedMapSystem _map = default!; // imp edit

    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    private HashSet<EntityUid> _toUpdate = new();
    private HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeedModifierContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SpeedModifierContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<SpeedModifiedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
        SubscribeLocalEvent<SpeedModifierContactsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpeedModifierContactsComponent, StepTriggeredOffEvent>(OnStepTriggered); // imp edit
        SubscribeLocalEvent<SpeedModifierContactsComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt); // imp edit

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
            RemComp<SpeedModifiedByContactComponent>(ent);
        }

        _toUpdate.Clear();
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
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid));
    }

    private void OnShutdown(EntityUid uid, SpeedModifierContactsComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        // Note that the entity may not be getting deleted here. E.g., glue puddles.
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
    }

    private void MovementSpeedCheck(EntityUid uid, SpeedModifiedByContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        var walkSpeed = 0.0f;
        var sprintSpeed = 0.0f;

        bool remove = true;
        var entries = 0;
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            // imp edit - StepTrigger and TryBlacklist checks
            if (TryComp<StepTriggerComponent>(ent, out var stepTriggerComponent) &&
                !TryBlacklist((ent, stepTriggerComponent)))
                continue;

            bool speedModified = false;

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
            _toRemove.Add(uid);
    }

    private void OnEntityExit(EntityUid uid, SpeedModifierContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        _toUpdate.Add(otherUid);
    }

    private void OnEntityEnter(EntityUid uid, SpeedModifierContactsComponent component, ref StartCollideEvent args)
    {
        // imp edit - added StepTrigger check
        if (HasComp<StepTriggerComponent>(uid))
            return;

        AddModifiedEntity(args.OtherEntity);
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
        _toUpdate.Add(uid);
    }

    // imp edit
    private void OnStepTriggered(Entity<SpeedModifierContactsComponent> ent, ref StepTriggeredOffEvent args)
    {
        AddModifiedEntity(args.Tripper);
    }

    // imp edit
    private static void OnStepTriggerAttempt(Entity<SpeedModifierContactsComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    // imp edit - copied from StepTriggerSystem, but converting that into a separate method is its own headache
    private bool TryBlacklist(Entity<StepTriggerComponent> ent)
    {
        if (!ent.Comp.Active ||
            ent.Comp.Colliding.Count == 0)
        {
            return true;
        }

        var transform = Transform(ent);

        if (ent.Comp.Blacklist == null || !TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return true;

        var pos = _map.LocalToTile(transform.GridUid.Value, grid, transform.Coordinates);
        var anch = _map.GetAnchoredEntitiesEnumerator(ent, grid, pos);

        while (anch.MoveNext(out var otherEnt))
        {
            if (otherEnt == ent)
                continue;

            if (_whitelistSystem.IsBlacklistPass(ent.Comp.Blacklist, otherEnt.Value))
            {
                return false;
            }
        }

        return true;
    }
}
