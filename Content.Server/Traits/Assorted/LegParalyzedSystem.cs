using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;

namespace Content.Server.Traits.Assorted;

public sealed class LegParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegParalyzedComponent, BuckleChangeEvent>(OnBuckleChange);
    }

    private void OnStartup(EntityUid uid, LegParalyzedComponent component, ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, 0, 0, 20, movementSpeed);
    }

    private void OnShutdown(EntityUid uid, LegParalyzedComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
        _bodySystem.UpdateMovementSpeed(uid);
    }

    private void OnBuckleChange(EntityUid uid, LegParalyzedComponent component, ref BuckleChangeEvent args)
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
