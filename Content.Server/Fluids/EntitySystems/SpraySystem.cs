using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Gravity;
using Content.Server.Popups;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Vapor;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Fluids.EntitySystems;
using Content.Shared.Fluids.Components;
using Robust.Server.Containers;
using Robust.Shared.Map;

namespace Content.Server.Fluids.EntitySystems;

public sealed class SpraySystem : SharedSpraySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly VaporSystem _vapor = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private float _gridImpulseMultiplier;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SprayComponent, UserActivateInWorldEvent>(OnActivateInWorld);
        Subs.CVar(_cfg, CCVars.GridImpulseMultiplier, UpdateGridMassMultiplier, true);
    }

    private void OnActivateInWorld(Entity<SprayComponent> entity, ref UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var targetMapPos = _transform.GetMapCoordinates(GetEntityQuery<TransformComponent>().GetComponent(args.Target));

        Spray(entity, targetMapPos, args.User);
    }

    private void UpdateGridMassMultiplier(float value)
    {
        _gridImpulseMultiplier = value;
    }

    private void OnAfterInteract(Entity<SprayComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var clickPos = _transform.ToMapCoordinates(args.ClickLocation);

        Spray(entity, clickPos, args.User);
    }

    public override void Spray(Entity<SprayComponent> entity, EntityUid? user = null)
    {
        var xform = Transform(entity);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.SprayDistance;
        var direction = xform.Coordinates.Offset(throwing);

        Spray(entity, _transform.ToMapCoordinates(direction), user);
    }

    public override void Spray(Entity<SprayComponent> entity, MapCoordinates mapcoord, EntityUid? user = null)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, SprayComponent.SolutionName, out var soln, out var solution))
            return;

        var ev = new SprayAttemptEvent(user);
        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
        {
            if (ev.CancelPopupMessage != null && user != null)
                _popupSystem.PopupEntity(Loc.GetString(ev.CancelPopupMessage), entity.Owner, user.Value);
            return;
        }

        if (_useDelay.IsDelayed((entity, null)))
            return;

        if (solution.Volume <= 0)
        {
            if (user != null)
                _popupSystem.PopupEntity(Loc.GetString(entity.Comp.SprayEmptyPopupMessage, ("entity", entity)), entity.Owner, user.Value);
            return;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sprayerXform = xformQuery.GetComponent(entity);

        var sprayerMapPos = _transform.GetMapCoordinates(sprayerXform);
        var clickMapPos = mapcoord;

        var diffPos = clickMapPos.Position - sprayerMapPos.Position;
        if (diffPos == Vector2.Zero || diffPos == Vector2Helpers.NaN)
            return;

        var diffNorm = diffPos.Normalized();
        var diffLength = diffPos.Length();

        if (diffLength > entity.Comp.SprayDistance)
        {
            diffLength = entity.Comp.SprayDistance;
        }

        var diffAngle = diffNorm.ToAngle();

        // Vectors to determine the spawn offset of the vapor clouds.
        var threeQuarters = diffNorm * 0.75f;
        var quarter = diffNorm * 0.25f;

        var amount = Math.Max(Math.Min((solution.Volume / entity.Comp.TransferAmount).Int(), entity.Comp.VaporAmount), 1);
        var spread = entity.Comp.VaporSpread / amount;

        for (var i = 0; i < amount; i++)
        {
            var rotation = new Angle(diffAngle + Angle.FromDegrees(spread * i) -
                                     Angle.FromDegrees(spread * (amount - 1) / 2));

            // Calculate the destination for the vapor cloud. Limit to the maximum spray distance.
            var target = sprayerMapPos
                .Offset((diffNorm + rotation.ToVec()).Normalized() * diffLength + quarter);

            var distance = (target.Position - sprayerMapPos.Position).Length();
            if (distance > entity.Comp.SprayDistance)
                target = sprayerMapPos.Offset(diffNorm * entity.Comp.SprayDistance);

            var adjustedSolutionAmount = entity.Comp.TransferAmount / entity.Comp.VaporAmount;
            var newSolution = _solutionContainer.SplitSolution(soln.Value, adjustedSolutionAmount);

            if (newSolution.Volume <= FixedPoint2.Zero)
                break;

            // Spawn the vapor cloud onto the grid/map the user is present on. Offset the start position based on how far the target destination is.
            var vaporPos = sprayerMapPos.Offset(distance < 1 ? quarter : threeQuarters);
            var vapor = Spawn(entity.Comp.SprayedPrototype, vaporPos);
            var vaporXform = xformQuery.GetComponent(vapor);

            _transform.SetWorldRotation(vaporXform, rotation);

            if (TryComp(vapor, out AppearanceComponent? appearance))
            {
                _appearance.SetData(vapor, VaporVisuals.Color, solution.GetColor(_proto).WithAlpha(1f), appearance);
                _appearance.SetData(vapor, VaporVisuals.State, true, appearance);
            }

            // Add the solution to the vapor and actually send the thing
            var vaporComponent = Comp<VaporComponent>(vapor);
            var ent = (vapor, vaporComponent);
            _vapor.TryAddSolution(ent, newSolution);

            // impulse direction is defined in world-coordinates, not local coordinates
            var impulseDirection = rotation.ToVec();
            var time = diffLength / entity.Comp.SprayVelocity;

            _vapor.Start(ent, vaporXform, impulseDirection * diffLength, entity.Comp.SprayVelocity, target, time, user);

            var thingGettingPushed = entity.Owner;
            if (_container.TryGetOuterContainer(entity, sprayerXform, out var container))
                thingGettingPushed = container.Owner;

            if (TryComp<PhysicsComponent>(thingGettingPushed, out var body))
            {
                if (_gravity.IsWeightless(thingGettingPushed))
                {
                    // push back the player
                    _physics.ApplyLinearImpulse(thingGettingPushed, -impulseDirection * entity.Comp.PushbackAmount, body: body);
                }
                else
                {
                    // push back the grid the player is standing on
                    var userTransform = Transform(thingGettingPushed);
                    if (userTransform.GridUid == userTransform.ParentUid)
                    {
                        // apply both linear and angular momentum depending on the player position
                        // multiply by a cvar because grid mass is currently extremely small compared to all other masses
                        _physics.ApplyLinearImpulse(userTransform.GridUid.Value, -impulseDirection * _gridImpulseMultiplier * entity.Comp.PushbackAmount, userTransform.LocalPosition);
                    }
                }
            }
        }

        _audio.PlayPvs(entity.Comp.SpraySound, entity, entity.Comp.SpraySound.Params.WithVariation(0.125f));

        _useDelay.TryResetDelay(entity);
    }
}
