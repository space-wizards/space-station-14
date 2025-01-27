using Content.Client._Impstation.Fluids.Components;
using Content.Shared._Impstation.Fluids;
using Content.Shared._Impstation.Fluids.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Events;

namespace Content.Client._Impstation.Fluids.EntitySystems;

public sealed partial class PropulsionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PropulsionComponent, StartCollideEvent>(StartColliding);
        SubscribeLocalEvent<PropulsionComponent, EndCollideEvent>(EndColliding);
        SubscribeLocalEvent<PropulsionComponent, ComponentHandleState>(OnSourceState);

        SubscribeLocalEvent<PropulsedByComponent, ComponentHandleState>(OnEntityState);
        SubscribeLocalEvent<PropulsedByComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnEntityState(EntityUid uid, PropulsedByComponent comp, ComponentHandleState args)
    {
        if (args.Current is not PropulsedByState state)
            return;

        comp.PredictFingerprint = state.PredictFingerprint;
        comp.WalkSpeedModifier = state.WalkSpeedModifier;
        comp.SprintSpeedModifier = state.SprintSpeedModifier;
    }

    private void OnSourceState(EntityUid uid, PropulsionComponent comp, ComponentHandleState args)
    {
        if (args.Current is not PropulsionState state)
            return;

        comp.PredictIndex = state.PredictIndex;
        comp.WalkSpeedModifier = state.WalkSpeedModifier;
        comp.SprintSpeedModifier = state.SprintSpeedModifier;
        comp.Whitelist = state.Whitelist;
    }

    private bool CanAffect(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!HasComp<InputMoverComponent>(other))
            return false;

        return _whitelist.IsWhitelistPassOrNull(entity.Comp.Whitelist, other);
    }

    private void StartAffecting(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!CanAffect(entity, other))
            return;

        var propulse = EnsureComp<PropulsedByComponent>(other);
        propulse.WalkSpeedModifier = entity.Comp.WalkSpeedModifier;
        propulse.SprintSpeedModifier = entity.Comp.SprintSpeedModifier;
        propulse.PredictFingerprint |= 1ul << entity.Comp.PredictIndex;
        Dirty(other, propulse);

        var modifier = EnsureComp<MovementSpeedModifierComponent>(other);
        _speed.RefreshMovementSpeedModifiers(other, modifier);
    }

    private void StopAffecting(Entity<PropulsionComponent> entity, EntityUid other)
    {
        if (!TryComp<PropulsedByComponent>(other, out var propulse))
            return;

        propulse.PredictFingerprint &= ~(1ul << entity.Comp.PredictIndex);
        Dirty(other, propulse);
        _speed.RefreshMovementSpeedModifiers(other);
    }

    private void StartColliding(EntityUid uid, PropulsionComponent comp, StartCollideEvent args)
    {
        StartAffecting((uid, comp), args.OtherEntity);
    }

    private void EndColliding(EntityUid uid, PropulsionComponent comp, EndCollideEvent args)
    {
        StopAffecting((uid, comp), args.OtherEntity);
    }

    private void OnRefreshSpeed(Entity<PropulsedByComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (entity.Comp.PredictFingerprint != 0)
        {
            args.ModifySpeed(entity.Comp.WalkSpeedModifier, entity.Comp.SprintSpeedModifier);
        }
    }
}
