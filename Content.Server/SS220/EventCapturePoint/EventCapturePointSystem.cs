// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.SS220.EventCapturePoint;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.SS220.EventCapturePoint;

public sealed class EventCapturePointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventCapturePointComponent, ActivateInWorldEvent>(OnActivated);
        SubscribeLocalEvent<EventCapturePointComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EventCapturePointComponent, ComponentShutdown>(OnPointShutdown);

        SubscribeLocalEvent<EventCapturePointFlagComponent, GettingPickedUpAttemptEvent>(OnFlagPickupAttempt);
        SubscribeLocalEvent<EventCapturePointComponent, FlagInstallationFinshedEvent>(OnFlagInstalled);
        SubscribeLocalEvent<EventCapturePointComponent, FlagRemovalFinshedEvent>(OnFlagRemoved);
    }

    #region Listeners
    private void OnActivated(Entity<EventCapturePointComponent> entity, ref ActivateInWorldEvent args)
    {
        if (entity.Comp.FlagEntity.HasValue)
        {
            RemoveFlag(entity, args.User);
        }
    }

    private void OnFlagInstalled(Entity<EventCapturePointComponent> entity, ref FlagInstallationFinshedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!args.Used.HasValue)
            return;

        AddFlagInstantly(entity, args.Used.Value);
    }

    private void OnFlagPickupAttempt(Entity<EventCapturePointFlagComponent> entity, ref GettingPickedUpAttemptEvent args)
    {
        if (entity.Comp.Planted)
            args.Cancel();
    }

    private void OnFlagRemoved(Entity<EventCapturePointComponent> entity, ref FlagRemovalFinshedEvent args)
    {
        if (args.Cancelled)
            return;

        RemoveFlagInstantly(entity);
    }

    private void OnPointShutdown(Entity<EventCapturePointComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.FlagEntity.HasValue &&
            entity.Comp.FlagEntity.Value.Valid &&
            EntityManager.EntityExists(entity.Comp.FlagEntity.Value))
        {
            RemoveFlagInstantly(entity);
        }
    }

    private void OnInteractUsing(Entity<EventCapturePointComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<EventCapturePointFlagComponent>(args.Used))
            return;

        AddOrRemoveFlag(entity, args.User, args.Used);
    }
    #endregion

    public void RemoveFlagInstantly(Entity<EventCapturePointComponent> entity)
    {
        if (entity.Comp.FlagEntity is not { } flag)
            return;

        if (TryComp<EventCapturePointFlagComponent>(flag, out var flagComp))
            flagComp.Planted = false;

        _transform.SetParent(flag, _transform.GetParentUid(entity));
        _appearance.SetData(flag, CaptureFlagVisuals.Visuals, false);
        _appearance.SetData(entity, CapturePointVisuals.Visuals, false);
        entity.Comp.FlagEntity = null;

        if (TryComp<PhysicsComponent>(flag, out var physComp))
        {
            _physics.SetBodyType(flag, BodyType.Dynamic, body: physComp);

            var maxAxisImp = entity.Comp.FlagRemovalImpulse;
            var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
            _physics.ApplyLinearImpulse(flag, impulseVec);
        }
    }

    public void AddFlagInstantly(Entity<EventCapturePointComponent> entity, EntityUid flag)
    {
        if (!TryComp<EventCapturePointFlagComponent>(flag, out var flagComp))
            return;

        _container.TryRemoveFromContainer(flag, true);
        var xform = EnsureComp<TransformComponent>(flag);
        var coords = new EntityCoordinates(entity, Vector2.Zero);
        _transform.SetCoordinates(flag, xform, coords);
        _transform.SetLocalRotationNoLerp(flag, Angle.Zero, xform);
        // We don't anchor an entity because that will lead to an unapplicable component state
        // because we can't remove an anchored entity from container
        flagComp.Planted = true;

        _appearance.SetData(flag, CaptureFlagVisuals.Visuals, true);
        _appearance.SetData(entity, CapturePointVisuals.Visuals, true);

        entity.Comp.FlagEntity = flag;

        if (TryComp<PhysicsComponent>(flag, out var physComp))
            _physics.SetBodyType(flag, BodyType.Static, body: physComp);
    }

    public void AddFlag(Entity<EventCapturePointComponent> entity, EntityUid user, EntityUid newFlag)
    {
        var flagEvent = new FlagInstallationFinshedEvent();

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.FlagManipulationDuration, flagEvent, entity, target: entity, used: newFlag)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    public void RemoveFlag(Entity<EventCapturePointComponent> entity, EntityUid user)
    {
        var flagEvent = new FlagRemovalFinshedEvent();

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.FlagManipulationDuration, flagEvent, entity, target: entity)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    public void AddOrRemoveFlag(Entity<EventCapturePointComponent> entity, EntityUid user, EntityUid newFlag)
    {
        if (entity.Comp.FlagEntity == newFlag)
            return;

        if (entity.Comp.FlagEntity.HasValue)
            RemoveFlag(entity, user);
        else
            AddFlag(entity, user, newFlag);
    }
}
