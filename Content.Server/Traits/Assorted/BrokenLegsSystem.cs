using Content.Shared.Buckle;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Traits.Assorted;

namespace Content.Server.Traits.Assorted;

public sealed class BrokenLegsSystem : SharedBrokenLegsSystem
{
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BrokenLegsComponent, ComponentStartup>(SetupBrokenLegs);
    }

    private void SetupBrokenLegs(EntityUid uid, BrokenLegsComponent component, ComponentStartup args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        // TODO: Is there a better way to break legs? I don't know him, so for now, that's the way it is.
        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, 0, 0, 20, movementSpeed);
        var carriage = Spawn("Carriage", Transform(uid).Coordinates);
        _buckleSystem.TryBuckle(uid, uid, carriage);
    }
}
