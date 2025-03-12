using Content.Server.Popups;
using Content.Server.Singularity.Events;
using Content.Shared.Shuttles.Components;
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
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
        SubscribeLocalEvent<ContainmentFieldComponent, EventHorizonAttemptConsumeEntityEvent>(HandleEventHorizon);
    }

    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, ref StartCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        if (component.DestroyGarbage && HasComp<SpaceGarbageComponent>(otherBody))
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-field-vaporized", ("entity", otherBody)), uid, PopupType.LargeCaution);
            QueueDel(otherBody);
        }

        if (TryComp<PhysicsComponent>(otherBody, out var physics) && physics.Mass <= component.MaxMass && physics.Hard)
        {
            var fieldDir = _transformSystem.GetWorldPosition(uid);
            var playerDir = _transformSystem.GetWorldPosition(otherBody);

            _throwing.TryThrow(otherBody, playerDir-fieldDir, baseThrowSpeed: component.ThrowForce);
        }
    }

    private void HandleEventHorizon(EntityUid uid, ContainmentFieldComponent component, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if(!args.Cancelled && !args.EventHorizon.CanBreachContainment)
            args.Cancelled = true;
    }
}
