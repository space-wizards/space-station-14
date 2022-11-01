using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
namespace Content.Shared.Traits.Assorted;

public sealed class HastySystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedStunSystem _stunnable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    [Dependency] private readonly IRobustRandom _random = default!;

    private bool _active = false;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HastyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HastyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HastyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<HastyComponent, MoveEvent>(OnMove);
    }

    private void OnShutdown(EntityUid uid, HastyComponent component, ComponentShutdown args)
    {
        _active = false;
        _movement.RefreshMovementSpeedModifiers(component.Owner);
    }

    private void OnStartup(EntityUid uid, HastyComponent component, ComponentStartup args)
    {
        component.LastSlipAttemptTime = DateTime.UtcNow;
    }

    private void OnMove(EntityUid uid, HastyComponent component, ref MoveEvent args)
    {
        // for some reason calling it in startup causes some issues with prediction??
        if (!_active)
        {
            _active = true;
            _movement.RefreshMovementSpeedModifiers(component.Owner);
        }

        // so that it doesn't try to slip you every frame
        if (component.LastSlipAttemptTime.AddSeconds(component.TrySlipInterval) >= DateTime.UtcNow)
            return;
        component.LastSlipAttemptTime = DateTime.UtcNow;

        // check if 0 gravity
        // it would be really funny if you could slip in 0g
        if (_gravity.IsWeightless(component.Owner))
            return;

        if (_random.NextFloat(0, 1) > component.ChanceOfSlip)
            return;

        // funny slip velocity which sends you forward
        if (TryComp(uid, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(physics, physics.LinearVelocity * component.LaunchForwardsMultiplier);

        double stunTime = _random.NextDouble(component.StunDuration.X, component.StunDuration.Y);
        _stunnable.TryParalyze(uid, TimeSpan.FromSeconds(stunTime), false);
    }

    private void OnRefreshMovespeed(EntityUid uid, HastyComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(_active ? component.MovementSpeedMultiplier : 1.0f, _active ? component.MovementSpeedMultiplier : 1.0f);
    }
}
