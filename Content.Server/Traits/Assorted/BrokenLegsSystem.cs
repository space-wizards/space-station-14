using Content.Server.Buckle.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Traits.Assorted;

public sealed class BrokenLegsSystem : EntitySystem
{
    [Dependency] private readonly BuckleSystem _buckleSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BrokenLegsComponent, ComponentStartup>(SetupBrokenLegs);
    }

    private void SetupBrokenLegs(EntityUid uid, BrokenLegsComponent component, ComponentStartup args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        // TODO: Is there a better way to break legs? I don't know him, so for now, that's the way it is.
        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, component.WalkSpeedModifier, component.SprintSpeedModifier,
            component.AccelerationSpeedModifier, movementSpeed);
        var carriage = Spawn(component.CarriageId, Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, carriage);
    }
}
