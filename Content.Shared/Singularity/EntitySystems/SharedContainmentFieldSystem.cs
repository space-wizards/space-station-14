using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// This handles the containment field for the singularity and tesla ball.
/// </summary>
public abstract class SharedContainmentFieldSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
    }

    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, ref StartCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        if (component.DestroyGarbage && HasComp<SpaceGarbageComponent>(otherBody))
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-field-vaporized", ("entity", otherBody)), uid, PopupType.LargeCaution);
            QueueDel(otherBody);
        }

        if (!TryComp<PhysicsComponent>(otherBody, out var physics) || physics.Mass > component.MaxMass || !physics.Hard)
            return;

        var fieldDir = _transformSystem.GetWorldPosition(uid);
        var playerDir = _transformSystem.GetWorldPosition(otherBody);
        var speed = Math.Min(component.ThrowForce / physics.Mass, component.MaxSpeed);

        _throwing.TryThrow(otherBody, playerDir-fieldDir, baseThrowSpeed: speed);
    }
}
