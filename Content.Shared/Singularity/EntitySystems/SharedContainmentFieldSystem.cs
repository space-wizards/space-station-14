using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// This handles the containment field for the singularity and tesla ball.
/// </summary>
public abstract class SharedContainmentFieldSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
    }

    private void HandleFieldCollide(Entity<ContainmentFieldComponent> entity, ref StartCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // TODO: When collisions stop being funky on client, remove the isServer check!
        if (entity.Comp.DestroyGarbage && HasComp<SpaceGarbageComponent>(otherBody))
        {
            if (_net.IsServer)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-field-vaporized", ("entity", otherBody)), entity, PopupType.LargeCaution);
                QueueDel(otherBody);
            }
            return;
        }

        if (entity.Comp.ThrowImpulse == 0)
            return;

        if (!TryComp<PhysicsComponent>(otherBody, out var physics) || physics.Mass > entity.Comp.MaxMass || !physics.Hard)
            return;

        var fieldDir = _transformSystem.GetWorldPosition(entity);
        var playerDir = _transformSystem.GetWorldPosition(otherBody);
        var speed = Math.Min(entity.Comp.ThrowImpulse / physics.Mass, entity.Comp.MaxSpeed);

        _throwing.TryThrow(otherBody, playerDir-fieldDir, baseThrowSpeed: speed, recoil: false);
    }
}
