using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Cooldown;
using Content.Server.Extinguisher;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared.Cooldown;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Vapor;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

public sealed class SpraySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly VaporSystem _vapor = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(FireExtinguisherSystem) });
    }

    private void OnAfterInteract(EntityUid uid, SprayComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_solutionContainer.TryGetSolution(uid, SprayComponent.SolutionName, out var solution))
            return;

        var ev = new SprayAttemptEvent(args.User);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        var curTime = _gameTiming.CurTime;
        if (TryComp<ItemCooldownComponent>(uid, out var cooldown)
            && curTime < cooldown.CooldownEnd)
            return;

        if (solution.Volume <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("spray-component-is-empty-message"), uid,
                args.User);
            return;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var userXform = xformQuery.GetComponent(args.User);

        var userMapPos = userXform.MapPosition;
        var clickMapPos = args.ClickLocation.ToMap(EntityManager);

        var diffPos = clickMapPos.Position - userMapPos.Position;
        if (diffPos == Vector2.Zero || diffPos == Vector2.NaN)
            return;

        var diffLength = diffPos.Length;
        var diffNorm = diffPos.Normalized;
        var diffAngle = diffNorm.ToAngle();

        // Vectors to determine the spawn offset of the vapor clouds.
        var threeQuarters = diffNorm * 0.75f;
        var quarter = diffNorm * 0.25f;

        var amount = Math.Max(Math.Min((solution.Volume / component.TransferAmount).Int(), component.VaporAmount), 1);
        var spread = component.VaporSpread / amount;

        for (var i = 0; i < amount; i++)
        {
            var rotation = new Angle(diffAngle + Angle.FromDegrees(spread * i) -
                                     Angle.FromDegrees(spread * (amount - 1) / 2));

            // Calculate the destination for the vapor cloud. Limit to the maximum spray distance.
            var target = userMapPos
                .Offset((diffNorm + rotation.ToVec()).Normalized * diffLength + quarter);

            var distance = target.Position.Length;
            if (distance > component.SprayDistance)
                target = userMapPos.Offset(diffNorm * component.SprayDistance);

            var newSolution = _solutionContainer.SplitSolution(uid, solution, component.TransferAmount);

            if (newSolution.Volume <= FixedPoint2.Zero)
                break;

            // Spawn the vapor cloud onto the grid/map the user is present on. Offset the start position based on how far the target destination is.
            var vaporPos = userMapPos.Offset(distance < 1 ? quarter : threeQuarters);
            var vapor = Spawn(component.SprayedPrototype, vaporPos);
            var vaporXform = xformQuery.GetComponent(vapor);

            _transform.SetWorldRotation(vaporXform, rotation);

            if (TryComp(vapor, out AppearanceComponent? appearance))
            {
                _appearance.SetData(vapor, VaporVisuals.Color, solution.GetColor(_proto).WithAlpha(1f), appearance);
                _appearance.SetData(vapor, VaporVisuals.State, true, appearance);
            }

            // Add the solution to the vapor and actually send the thing
            var vaporComponent = Comp<VaporComponent>(vapor);
            _vapor.TryAddSolution(vaporComponent, newSolution);

            // impulse direction is defined in world-coordinates, not local coordinates
            var impulseDirection = rotation.ToVec();
            _vapor.Start(vaporComponent, vaporXform, impulseDirection, component.SprayVelocity, target, component.SprayAliveTime, args.User);
        }

        _audio.PlayPvs(component.SpraySound, uid, component.SpraySound.Params.WithVariation(0.125f));

        RaiseLocalEvent(uid,
            new RefreshItemCooldownEvent(curTime, curTime + TimeSpan.FromSeconds(component.CooldownTime)), true);
    }
}

public sealed class SprayAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public SprayAttemptEvent(EntityUid user)
    {
        User = user;
    }
}
