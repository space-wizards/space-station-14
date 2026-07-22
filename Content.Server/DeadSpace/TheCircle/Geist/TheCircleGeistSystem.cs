// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Stealth;
using Content.Shared.Actions;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DeadSpace.Carrying;
using Content.Shared.DeadSpace.TheCircle.Geist;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.DeadSpace.TheCircle.Geist;

public sealed class TheCircleGeistSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TheCircleGeistComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TheCircleGeistComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TheCircleGeistComponent, OpenGeistInvisibilityMenuEvent>(OnOpenMenu);
        SubscribeLocalEvent<TheCircleGeistComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeNetworkEvent<SelectGeistInvisibilityModeEvent>(OnSelectMode);
    }

    private void OnMapInit(Entity<TheCircleGeistComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.InvisibilityActionEntity, ent.Comp.InvisibilityAction, ent.Owner);
    }

    private void OnShutdown(Entity<TheCircleGeistComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.InvisibilityActionEntity);
        EndEffect(ent, false);
    }

    private void OnOpenMenu(Entity<TheCircleGeistComponent> ent, ref OpenGeistInvisibilityMenuEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.ActiveMode == GeistInvisibilityMode.Stationary)
        {
            EndEffect(ent, true);
            return;
        }

        if (ent.Comp.ActiveMode != null)
            return;

        ent.Comp.ModeSelectionExpires = _timing.CurTime + TimeSpan.FromSeconds(10);
        Dirty(ent);
    }

    private void OnSelectMode(SelectGeistInvisibilityModeEvent args, EntitySessionEventArgs session)
    {
        var uid = GetEntity(args.Geist);
        if (session.SenderSession.AttachedEntity != uid || !TryComp<TheCircleGeistComponent>(uid, out var component))
            return;

        if (component.ActiveMode != null ||
            component.ModeSelectionExpires == null ||
            _timing.CurTime > component.ModeSelectionExpires ||
            !IsModeReady(component, args.Mode))
            return;

        component.ModeSelectionExpires = null;
        StartEffect((uid, component), args.Mode);
    }

    private void StartEffect(Entity<TheCircleGeistComponent> ent, GeistInvisibilityMode mode)
    {
        ent.Comp.ActiveMode = mode;
        ent.Comp.MovementStarted = null;
        ent.Comp.ModeSelectionExpires = null;
        var stealth = EnsureComp<StealthComponent>(ent);
        _stealth.SetEnabled(ent, true, stealth);
        _stealth.SetVisibility(ent, mode == GeistInvisibilityMode.Stationary
            ? ent.Comp.MovingVisibility
            : ent.Comp.FullVisibility, stealth);

        switch (mode)
        {
            case GeistInvisibilityMode.Escape:
                ent.Comp.EffectEnds = _timing.CurTime + ent.Comp.EscapeDuration;
                break;
            case GeistInvisibilityMode.Phase:
                ent.Comp.EffectEnds = _timing.CurTime + ent.Comp.PhaseDuration;
                ent.Comp.PhaseLastSafeCoordinates = Transform(ent).Coordinates;
                if (!IsCarryingEntity(ent))
                    DisableCollision(ent);
                break;
            case GeistInvisibilityMode.Stationary:
                ent.Comp.EffectEnds = null;
                SetAmbushPacified(ent, true);
                ent.Comp.HadStealthOnMove = TryComp<StealthOnMoveComponent>(ent, out var oldStealthOnMove);
                if (oldStealthOnMove != null)
                {
                    ent.Comp.OriginalPassiveVisibilityRate = oldStealthOnMove.PassiveVisibilityRate;
                    ent.Comp.OriginalMovementVisibilityRate = oldStealthOnMove.MovementVisibilityRate;
                }
                var stealthOnMove = EnsureComp<StealthOnMoveComponent>(ent);
                stealthOnMove.PassiveVisibilityRate = -ent.Comp.StationaryFadeRate;
                stealthOnMove.MovementVisibilityRate = ent.Comp.StationaryMovementVisibilityRate;
                Dirty(ent, stealthOnMove);
                break;
        }

        _movement.RefreshMovementSpeedModifiers(ent);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TheCircleGeistComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var component, out var physics))
        {
            if (component.ActiveMode == GeistInvisibilityMode.Phase)
                UpdatePhaseCollision((uid, component));

            if (component.ActiveMode == GeistInvisibilityMode.Phase &&
                _physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count == 0)
            {
                component.PhaseLastSafeCoordinates = Transform(uid).Coordinates;
            }

            if (component.ActiveMode == null)
                continue;

            var moving = physics.LinearVelocity.LengthSquared() > component.MovementThreshold * component.MovementThreshold;
            if (component.ActiveMode == GeistInvisibilityMode.Escape)
            {
                SetVisibility(uid, component.EscapeRunningVisibility);
            }
            else if (component.ActiveMode == GeistInvisibilityMode.Stationary)
            {
                if (moving)
                {
                    component.MovementStarted ??= _timing.CurTime;
                    SetAmbushPacified((uid, component), false);
                }
                else
                {
                    component.MovementStarted = null;
                    SetAmbushPacified((uid, component), true);
                }

                if (component.MovementStarted != null &&
                    _timing.CurTime >= component.MovementStarted + component.StationaryMovementGrace)
                {
                    EndEffect((uid, component), true);
                    continue;
                }
            }

            if (component.EffectEnds != null && _timing.CurTime >= component.EffectEnds)
                EndEffect((uid, component), true);
        }
    }

    private void SetVisibility(EntityUid uid, float value)
    {
        if (TryComp<StealthComponent>(uid, out var stealth))
            _stealth.SetVisibility(uid, value, stealth);
    }

    private void EndEffect(Entity<TheCircleGeistComponent> ent, bool startCooldown)
    {
        var oldMode = ent.Comp.ActiveMode;
        if (oldMode == GeistInvisibilityMode.Phase && TryComp<PhysicsComponent>(ent, out var physics))
        {
            var stuck = _physics.GetEntitiesIntersectingBody(ent, (int) CollisionGroup.Impassable).Count > 0;
            RestoreCollision(ent);

            if (stuck)
            {
                if (ent.Comp.PhaseLastSafeCoordinates is { } safeCoordinates)
                    _transform.SetCoordinates(ent, Transform(ent), safeCoordinates);

                _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
                _stun.TryUpdateParalyzeDuration(ent, ent.Comp.PhaseStunDuration);
            }

            ent.Comp.PhaseLastSafeCoordinates = null;
        }

        if (oldMode == GeistInvisibilityMode.Stationary)
            SetAmbushPacified(ent, false);

        if (oldMode == GeistInvisibilityMode.Stationary && TryComp<StealthOnMoveComponent>(ent, out var stealthOnMove))
        {
            if (ent.Comp.HadStealthOnMove)
            {
                stealthOnMove.PassiveVisibilityRate = ent.Comp.OriginalPassiveVisibilityRate;
                stealthOnMove.MovementVisibilityRate = ent.Comp.OriginalMovementVisibilityRate;
                Dirty(ent, stealthOnMove);
            }
            else
            {
                RemCompDeferred<StealthOnMoveComponent>(ent);
            }
        }
        RemComp<StealthComponent>(ent);
        ent.Comp.ActiveMode = null;
        ent.Comp.EffectEnds = null;
        ent.Comp.MovementStarted = null;
        _movement.RefreshMovementSpeedModifiers(ent);

        if (startCooldown && oldMode is { } mode)
        {
            var readyAt = mode switch
            {
                GeistInvisibilityMode.Escape => _timing.CurTime + ent.Comp.EscapeCooldown,
                GeistInvisibilityMode.Phase => _timing.CurTime + ent.Comp.PhaseCooldown,
                _ => _timing.CurTime + ent.Comp.StationaryCooldown,
            };

            switch (mode)
            {
                case GeistInvisibilityMode.Escape:
                    ent.Comp.EscapeReadyAt = readyAt;
                    break;
                case GeistInvisibilityMode.Phase:
                    ent.Comp.PhaseReadyAt = readyAt;
                    break;
                case GeistInvisibilityMode.Stationary:
                    ent.Comp.StationaryReadyAt = readyAt;
                    break;
            }
        }

        Dirty(ent);
    }

    private void SetAmbushPacified(Entity<TheCircleGeistComponent> ent, bool enabled)
    {
        if (enabled)
        {
            if (HasComp<PacifiedComponent>(ent))
                return;

            AddComp<PacifiedComponent>(ent);
            ent.Comp.AmbushPacifismApplied = true;
            return;
        }

        if (!ent.Comp.AmbushPacifismApplied)
            return;

        RemComp<PacifiedComponent>(ent);
        ent.Comp.AmbushPacifismApplied = false;
    }

    private void DisableCollision(Entity<TheCircleGeistComponent> ent)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physics) ||
            !TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        ent.Comp.WasCollidable = physics.CanCollide;
        ent.Comp.OriginalCollisionMasks.Clear();
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            ent.Comp.OriginalCollisionMasks[id] = fixture.CollisionMask;
            _physics.SetCollisionMask(ent, id, fixture, 0, fixtures, physics);
        }
    }

    private void RestoreCollision(Entity<TheCircleGeistComponent> ent)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physics) ||
            !TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        foreach (var (id, mask) in ent.Comp.OriginalCollisionMasks)
        {
            if (fixtures.Fixtures.TryGetValue(id, out var fixture))
                _physics.SetCollisionMask(ent, id, fixture, mask, fixtures, physics);
        }

        ent.Comp.OriginalCollisionMasks.Clear();
    }

    private void UpdatePhaseCollision(Entity<TheCircleGeistComponent> ent)
    {
        if (IsCarryingEntity(ent))
        {
            if (ent.Comp.OriginalCollisionMasks.Count > 0)
                RestoreCollision(ent);
            return;
        }

        if (ent.Comp.OriginalCollisionMasks.Count == 0)
            DisableCollision(ent);
    }

    private bool IsCarryingEntity(EntityUid uid)
    {
        return TryComp<CarryingComponent>(uid, out var carrying) && carrying.Carried != null;
    }

    private bool IsModeReady(TheCircleGeistComponent component, GeistInvisibilityMode mode)
    {
        return _timing.CurTime >= (mode switch
        {
            GeistInvisibilityMode.Escape => component.EscapeReadyAt,
            GeistInvisibilityMode.Phase => component.PhaseReadyAt,
            _ => component.StationaryReadyAt,
        });
    }

    private void OnRefreshSpeed(Entity<TheCircleGeistComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        switch (ent.Comp.ActiveMode)
        {
            case GeistInvisibilityMode.Escape:
                args.ModifySpeed(ent.Comp.EscapeSpeedModifier);
                break;
            case GeistInvisibilityMode.Phase:
                args.ModifySpeed(ent.Comp.PhaseSpeedModifier);
                break;
        }
    }
}
