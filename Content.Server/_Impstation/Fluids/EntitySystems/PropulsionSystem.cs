using System.Linq;
using Content.Server._Impstation.Fluids.Components;
using Content.Shared._Impstation.Fluids;
using Content.Shared._Impstation.Fluids.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Fluids.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.Fluids;

public sealed partial class PropulsionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    private byte _nextPredictIndex = 0;
    private const ulong MAX_PREDICT_INDEX = 8 * sizeof(ulong);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PropulsedByComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<PropulsedByComponent, ComponentShutdown>(OnEntityShutdown);
        SubscribeLocalEvent<PropulsedByComponent, ComponentGetState>(OnEntityState);

        SubscribeLocalEvent<PropulsionComponent, StartCollideEvent>(StartColliding);
        SubscribeLocalEvent<PropulsionComponent, EndCollideEvent>(EndColliding);
        SubscribeLocalEvent<PropulsionComponent, ComponentShutdown>(OnSourceShutdown);
        SubscribeLocalEvent<PropulsionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PropulsionComponent, ComponentGetState>(OnSourceState);

        SubscribeLocalEvent<RoundRestartCleanupEvent>((_) => _nextPredictIndex = 0);
    }

    private void OnEntityState(Entity<PropulsedByComponent> entity, ref ComponentGetState args)
    {
        var modifier = entity.Comp.Sources.FirstOrNull();
        var fingerprint = entity.Comp.Sources.Aggregate(0ul, (fingerprint, source) =>
        {
            return fingerprint | (1ul << source.Comp.PredictIndex);
        });
        args.State = new PropulsedByState()
        {
            WalkSpeedModifier = modifier?.Comp.WalkSpeedModifier ?? 1f,
            SprintSpeedModifier = modifier?.Comp.SprintSpeedModifier ?? 1f,
            PredictFingerprint = fingerprint,
        };
    }

    private void OnSourceState(Entity<PropulsionComponent> entity, ref ComponentGetState args)
    {
        args.State = new PropulsionState(entity);
    }

    private void OnStartup(EntityUid uid, PropulsionComponent comp, ComponentStartup args)
    {
        if (comp.LifeStage != ComponentLifeStage.Starting)
            return;

        comp.PredictIndex = (byte)(_nextPredictIndex % MAX_PREDICT_INDEX);
        _nextPredictIndex += 1;

        Dirty(uid, comp);
    }

    private void OnSourceShutdown(EntityUid uid, PropulsionComponent comp, ComponentShutdown args)
    {
        foreach (var other in comp.AffectingEntities)
        {
            other.Comp.Sources.Remove((uid, comp));
            Dirty(other);
        }
    }

    private void OnEntityShutdown(EntityUid uid, PropulsedByComponent comp, ComponentShutdown args)
    {
        foreach (var source in comp.Sources)
        {
            source.Comp.AffectingEntities.Remove((uid, comp));
            Dirty(source);
        }
    }

    private bool CanAffect(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!HasComp<InputMoverComponent>(other))
            return false;

        return _whitelist.IsWhitelistPassOrNull(entity.Comp.Whitelist, other);
    }

    private void StartColliding(EntityUid uid, PropulsionComponent comp, StartCollideEvent args)
    {
        StartAffecting((uid, comp), args.OtherEntity);
    }

    private void EndColliding(EntityUid uid, PropulsionComponent comp, EndCollideEvent args)
    {
        StopAffecting((uid, comp), args.OtherEntity);
    }

    private void StartAffecting(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!CanAffect(entity, other))
            return;

        var propulse = EnsureComp<PropulsedByComponent>(other);
        if (propulse.Sources.Add(entity))
            Dirty(other, propulse);
        entity.Comp.AffectingEntities.Add((other, propulse));
        var modifier = EnsureComp<MovementSpeedModifierComponent>(other);
        _speed.RefreshMovementSpeedModifiers(other, modifier);

    }

    private void StopAffecting(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!TryComp<PropulsedByComponent>(other, out var propulse))
            return;

        if (propulse.Sources.Remove(entity))
            Dirty(other, propulse);
        entity.Comp.AffectingEntities.Remove((other, propulse));
        _speed.RefreshMovementSpeedModifiers(other);
    }

    public void OnRefreshSpeed(Entity<PropulsedByComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Sources.Count == 0)
        {
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        var modifier = ent.Comp.Sources.First();
        args.ModifySpeed(modifier.Comp.WalkSpeedModifier, modifier.Comp.SprintSpeedModifier);
    }
}
