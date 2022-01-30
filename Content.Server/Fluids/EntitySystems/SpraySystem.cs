using System;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Cooldown;
using Content.Server.Extinguisher;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Cooldown;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Vapor;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

public sealed class SpraySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly VaporSystem _vaporSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayComponent, AfterInteractEvent>(OnAfterInteract, after: new []{ typeof(FireExtinguisherSystem) });
    }

    private void OnAfterInteract(EntityUid uid, SprayComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_solutionContainerSystem.TryGetSolution(uid, SprayComponent.SolutionName, out var solution))
            return;

        var ev = new SprayAttemptEvent(args.User);
        RaiseLocalEvent(uid, ev, false);
        if (ev.Cancelled)
            return;

        var curTime = _gameTiming.CurTime;
        if (TryComp<ItemCooldownComponent>(uid, out var cooldown)
            && curTime < cooldown.CooldownEnd)
            return;

        if (solution.CurrentVolume <= 0)
        {
            _popupSystem.PopupEntity( Loc.GetString("spray-component-is-empty-message"),uid,
                Filter.Entities(args.User));
            return;
        }

        var playerPos = Transform(args.User).Coordinates;

        if (args.ClickLocation.GetGridId(EntityManager) != playerPos.GetGridId(EntityManager))
            return;

        var direction = (args.ClickLocation.Position - playerPos.Position).Normalized;
        var threeQuarters = direction * 0.75f;
        var quarter = direction * 0.25f;

        var amount = Math.Max(Math.Min((solution.CurrentVolume / component.TransferAmount).Int(), component.VaporAmount), 1);

        var spread = component.VaporSpread / amount;

        for (var i = 0; i < amount; i++)
        {
            var rotation = new Angle(direction.ToAngle() + Angle.FromDegrees(spread * i) -
                                     Angle.FromDegrees(spread * (amount - 1) / 2));

            var (_, diffPos) = args.ClickLocation - playerPos;
            var diffNorm = diffPos.Normalized;
            var diffLength = diffPos.Length;

            var target = Transform(args.User).Coordinates
                .Offset((diffNorm + rotation.ToVec()).Normalized * diffLength + quarter);

            if (target.TryDistance(EntityManager, playerPos, out var distance) && distance > component.SprayDistance)
                target = Transform(args.User).Coordinates
                    .Offset(diffNorm * component.SprayDistance);

            var newSolution = _solutionContainerSystem.SplitSolution(uid, solution, component.TransferAmount);

            if (newSolution.TotalVolume <= FixedPoint2.Zero)
                break;

            var vapor = Spawn(component.SprayedPrototype,
                playerPos.Offset(distance < 1 ? quarter : threeQuarters));
            Transform(vapor).LocalRotation = rotation;

            if (TryComp(vapor, out AppearanceComponent? appearance))
            {
                appearance.SetData(VaporVisuals.Color, solution.Color.WithAlpha(1f));
                appearance.SetData(VaporVisuals.State, true);
            }

            // Add the solution to the vapor and actually send the thing
            var vaporComponent = Comp<VaporComponent>(vapor);
            _vaporSystem.TryAddSolution(vaporComponent, newSolution);

            // impulse direction is defined in world-coordinates, not local coordinates
            var impulseDirection = Transform(vapor).WorldRotation.ToVec();
            _vaporSystem.Start(vaporComponent, impulseDirection, component.SprayVelocity, target, component.SprayAliveTime);

            if (component.Impulse > 0f && TryComp(args.User, out PhysicsComponent? body))
                body.ApplyLinearImpulse(-impulseDirection * component.Impulse);
        }

        SoundSystem.Play(Filter.Pvs(uid), component.SpraySound.GetSound(), uid, AudioHelpers.WithVariation(0.125f));

        RaiseLocalEvent(uid,
            new RefreshItemCooldownEvent(curTime, curTime + TimeSpan.FromSeconds(component.CooldownTime)));
    }
}

public class SprayAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public SprayAttemptEvent(EntityUid user)
    {
        User = user;
    }
}
