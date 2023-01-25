using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Singularity.Events;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
        SubscribeLocalEvent<ContainmentFieldComponent, EventHorizonAttemptConsumeEntityEvent>(HandleEventHorizon);
    }

    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, ref StartCollideEvent args)
    {
        var otherBody = args.OtherFixture.Body.Owner;

        if (TryComp<SpaceGarbageComponent>(otherBody, out var garbage))
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-field-vaporized", ("entity", otherBody)), component.Owner, PopupType.LargeCaution);
            QueueDel(garbage.Owner);
        }

        if (TryComp<PhysicsComponent>(otherBody, out var physics) && physics.Mass <= component.MaxMass && physics.Hard)
        {
            var fieldDir = Transform(component.Owner).WorldPosition;
            var playerDir = Transform(otherBody).WorldPosition;

            _throwing.TryThrow(otherBody, playerDir-fieldDir, strength: component.ThrowForce);
        }
    }

    private void HandleEventHorizon(EntityUid uid, ContainmentFieldComponent component, EventHorizonAttemptConsumeEntityEvent args)
    {
        if(!args.Cancelled && !args.EventHorizon.CanBreachContainment)
            args.Cancel();
    }
}
