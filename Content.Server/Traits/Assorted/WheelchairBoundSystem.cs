using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Traits.Assorted;

namespace Content.Server.Traits.Assorted;

public sealed class WheelchairBoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WheelchairBoundComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WheelchairBoundComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WheelchairBoundComponent, BuckleChangeEvent>(OnBuckleChange);
    }

    private void OnStartup(EntityUid uid, WheelchairBoundComponent component, ComponentStartup args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        // TODO: Is there a better way to break legs? I don't know him, so for now, that's the way it is.
        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, 0, 0, 20, movementSpeed);
        var wheelchair = Spawn(component.WheelchairPrototype, Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, wheelchair);
    }

    private void OnShutdown(EntityUid uid, WheelchairBoundComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
        _bodySystem.UpdateMovementSpeed(uid);
    }

    private void OnBuckleChange(EntityUid uid, WheelchairBoundComponent component, ref BuckleChangeEvent args)
    {
        if (args.Buckling)
        {
            _standingSystem.Stand(args.BuckledEntity);
        }
        else
        {
            _standingSystem.Down(args.BuckledEntity);
        }
    }
}
