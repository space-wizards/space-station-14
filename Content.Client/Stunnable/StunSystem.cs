using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Rotation;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client.Stunnable;

public sealed class StunSystem : SharedStunSystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly InputSystem _input = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockedDownComponent, MoveEvent>(OnMovementInput);

        CommandBinds.Builder
            .BindAfter(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true), typeof(SharedInteractionSystem))
            .Register<StunSystem>();
    }

    private bool OnUseSecondary(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not { Valid: true } uid)
            return false;

        if (_input.Predicted)
            return false;

        if (args.EntityUid != uid || !HasComp<KnockedDownComponent>(uid) || !_combat.IsInCombatMode(uid))
            return false;

        RaisePredictiveEvent(new ForceStandUpEvent());
        return true;
    }

    private void OnMovementInput(EntityUid uid, KnockedDownComponent component, MoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || _animation.HasRunningAnimation(uid, "rotate")
            || !TryComp<RotationVisualsComponent>(uid, out var rotationVisuals))
            return;

        var rotation = Transform(uid).LocalRotation + (_eyeManager.CurrentEye.Rotation - (Transform(uid).LocalRotation - _transformSystem.GetWorldRotation(uid)));

        if (rotation.GetDir() is Direction.SouthEast or Direction.East or Direction.NorthEast or Direction.North)
        {
            rotationVisuals.HorizontalRotation = Angle.FromDegrees(270);
            _sprites.SetRotation(uid, Angle.FromDegrees(270));
        }
        else
        {
            rotationVisuals.HorizontalRotation = Angle.FromDegrees(90);
            _sprites.SetRotation(uid, Angle.FromDegrees(90));
        }
    }
}

