using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class MovementRenderTest : MovementTest
{
    protected override int Tiles => 0;
    protected override bool AddWalls => false;

    [Test]
    public async Task InputMoverRotationSnapsWithoutSnappingPositionTest()
    {
        await Client.WaitAssertion(() =>
        {
            var transforms = CEntMan.System<TransformSystem>();
            var xform = CEntMan.GetComponent<TransformComponent>(CPlayer);
            var sourcePosition = xform.LocalPosition;
            var sourceWorldPosition = transforms.GetWorldPosition(CPlayer);
            var targetPosition = sourcePosition + Vector2.UnitX;
            var targetRotation = xform.LocalRotation + Angle.FromDegrees(90);
            transforms.ResetRenderPoses();

            using (CGameTiming.StartStateApplicationArea())
                transforms.SetLocalPositionRotation(CPlayer, targetPosition, targetRotation, xform);

            var inputMover = CEntMan.GetComponent<InputMoverComponent>(CPlayer);
            var moveInput = new MoveInputEvent((CPlayer, inputMover), inputMover.HeldMoveButtons);
            CEntMan.EventBus.RaiseLocalEvent(CPlayer, ref moveInput);

            Assert.Multiple(() =>
            {
                Assert.That(xform.LocalPosition, Is.EqualTo(targetPosition));
                Assert.That(xform.LocalRotation.EqualsApprox(targetRotation), Is.True);
                Assert.That(transforms.GetRenderWorldPosition(CPlayer), Is.EqualTo(sourceWorldPosition));
                Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(
                    transforms.GetWorldRotation(CPlayer)), Is.True);
                Assert.That(transforms.TryGetRenderPoseDebugData(CPlayer, out _), Is.True);
            });
        });
    }

    [Test]
    public async Task OrdinaryHeldMovementStaysCorrectionFreeTest()
    {
        await OverrideCVar(Side.Client, CVars.NetPredictTickBias, 12);
        await RunTicks(5);

        await Client.WaitPost(() => CEntMan.System<TransformSystem>().ResetRenderPoses());
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Down);

        var captures = new List<RotationCapture>();
        for (var tick = 0; tick < 6; tick++)
        {
            await Client.WaitRunTicks(1);
            for (var frame = 1; frame <= 3; frame++)
            {
                var capture = await CaptureRotation(tick, frame);
                captures.Add(capture);
                AssertOrdinaryMovement(capture, captures);
            }
        }

        for (var tick = 6; tick < 30; tick++)
        {
            await Server.WaitRunTicks(1);
            var afterState = await CaptureRotation(tick, 0);
            captures.Add(afterState);
            AssertOrdinaryMovement(afterState, captures);

            await Client.WaitRunTicks(1);
            for (var frame = 1; frame <= 3; frame++)
            {
                var capture = await CaptureRotation(tick, frame);
                captures.Add(capture);
                AssertOrdinaryMovement(capture, captures);
            }
        }

        await SetMovementKey(DirectionFlag.East, BoundKeyState.Up);
        await RunTicks(3);
    }

    [Test]
    public async Task ClickToFaceSnapsRenderRotationTest()
    {
        await OverrideCVar(Side.Client, CVars.NetPredictTickBias, 8);
        await RunTicks(5);

        NetCoordinates target = default;
        var initialRotation = Angle.Zero;
        var clickedRotation = Angle.Zero;
        var authoritativeRotation = Angle.Zero;
        await Client.WaitPost(() =>
        {
            var transforms = CEntMan.System<TransformSystem>();
            transforms.ResetRenderPoses();
            initialRotation = transforms.GetWorldRotation(CPlayer);
            var xform = CEntMan.GetComponent<TransformComponent>(CPlayer);
            target = CEntMan.GetNetCoordinates(xform.Coordinates.Offset(new Vector2(2f, 1f)));
        });

        await SetKey(EngineKeyFunctions.Use, BoundKeyState.Down, target);
        await Client.WaitAssertion(() =>
        {
            var timing = Client.ResolveDependency<IClientGameTiming>();
            timing.InSimulation = false;
            try
            {
                timing.TickRemainder = timing.TickPeriod / 4;
                CEntMan.FrameUpdate(1f / 60f);

                var transforms = CEntMan.System<TransformSystem>();
                var simulation = transforms.GetWorldRotation(CPlayer);
                Assert.That(simulation.EqualsApprox(initialRotation), Is.False);
                Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(simulation), Is.True);
                clickedRotation = simulation;
            }
            finally
            {
                timing.InSimulation = true;
            }
        });

        await Server.WaitPost(() =>
        {
            // Replay the click against an already-correct authoritative rotation.
            var xform = SEntMan.GetComponent<TransformComponent>(SPlayer);
            var targetPosition = Transform.ToMapCoordinates(ToServer(target)).Position;
            var position = Transform.GetWorldPosition(SPlayer) + new Vector2(0f, 0.75f);
            Transform.SetWorldPosition((SPlayer, xform), position);
            authoritativeRotation = Angle.FromWorldVec(targetPosition - position);
            Transform.SetWorldRotation(SPlayer, authoritativeRotation);
        });

        for (var tick = 0; tick < 12; tick++)
        {
            await Server.WaitRunTicks(1);
            await Client.WaitRunTicks(1);
            await Client.WaitAssertion(() =>
            {
                var timing = Client.ResolveDependency<IClientGameTiming>();
                timing.InSimulation = false;
                try
                {
                    for (var frame = 1; frame <= 3; frame++)
                    {
                        timing.TickRemainder = timing.TickPeriod * frame / 4;
                        CEntMan.FrameUpdate(1f / 144f);

                        var transforms = CEntMan.System<TransformSystem>();
                        var simulation = transforms.GetWorldRotation(CPlayer);
                        var hasDebugData = transforms.TryGetRenderPoseDebugData(CPlayer, out var debugData);
                        Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(simulation), Is.True,
                            $"tick {tick}, frame {frame}, clicked {clickedRotation}, simulation {simulation}, data {debugData}");
                        if (hasDebugData)
                        {
                            Assert.That(Math.Abs(debugData.CorrectionRotation.Theta), Is.LessThan(0.0001),
                                $"tick {tick}, frame {frame}");
                        }
                    }
                }
                finally
                {
                    timing.InSimulation = true;
                }
            });
        }

        await Client.WaitAssertion(() =>
        {
            var transforms = CEntMan.System<TransformSystem>();
            Assert.That(transforms.GetWorldRotation(CPlayer).EqualsApprox(authoritativeRotation), Is.True);
        });

        await SetKey(EngineKeyFunctions.Use, BoundKeyState.Up, target);
        await RunTicks(1);
    }

    [Test]
    public async Task RapidClickFacingStaysOnLatestRotationTest()
    {
        await OverrideCVar(Side.Client, CVars.NetPredictTickBias, 12);
        await RunTicks(5);

        var targets = new NetCoordinates[2];
        await Client.WaitPost(() =>
        {
            var transforms = CEntMan.System<TransformSystem>();
            transforms.ResetRenderPoses();
            var coordinates = CEntMan.GetComponent<TransformComponent>(CPlayer).Coordinates;
            targets[0] = CEntMan.GetNetCoordinates(coordinates.Offset(Vector2.UnitX * 2f));
            targets[1] = CEntMan.GetNetCoordinates(coordinates.Offset(-Vector2.UnitX * 2f));
        });

        var captures = new List<RotationCapture>();
        var targetIndex = 0;
        var finalRotation = Angle.Zero;
        await SetKey(EngineKeyFunctions.Use, BoundKeyState.Down, targets[targetIndex]);
        await SetKey(EngineKeyFunctions.Use, BoundKeyState.Up, targets[targetIndex]);

        const int changes = 6;
        for (var change = 0; change < changes; change++)
        {
            targetIndex ^= 1;
            await SetKey(EngineKeyFunctions.Use, BoundKeyState.Down, targets[targetIndex]);
            await SetKey(EngineKeyFunctions.Use, BoundKeyState.Up, targets[targetIndex]);

            var expected = Angle.Zero;
            await Client.WaitPost(() =>
                expected = CEntMan.System<TransformSystem>().GetWorldRotation(CPlayer));
            finalRotation = expected;

            var immediate = await CaptureRotation(change, 0);
            captures.Add(immediate);
            AssertRotationSnapped(expected, immediate, captures);

            await Client.WaitRunTicks(1);
            for (var frame = 1; frame <= 3; frame++)
            {
                var capture = await CaptureRotation(change, frame);
                captures.Add(capture);
                AssertRotationSnapped(expected, capture, captures);
            }
        }

        for (var settle = 0; settle < 16; settle++)
        {
            await Server.WaitRunTicks(1);
            await Client.WaitRunTicks(1);
            for (var frame = 1; frame <= 3; frame++)
            {
                var capture = await CaptureRotation(changes + settle, frame);
                captures.Add(capture);
                AssertRotationSnapped(finalRotation, capture, captures);
            }
        }
    }

    [Test]
    public async Task CombatModeExitKeepsDisplayedRotationTest()
    {
        await OverrideCVar(Side.Client, CVars.NetPredictTickBias, 12);
        await SetCombatMode(true);
        await RunTicks(5);

        var captures = new List<RotationCapture>();
        const int changes = 12;
        for (var change = 0; change < changes; change++)
        {
            var direction = (change & 1) == 0 ? Vector2.UnitX : -Vector2.UnitX;
            await Client.WaitPost(() => SetMouseDirection(direction));
            await Client.WaitRunTicks(1);

            var capture = await CaptureRotation(change, 0);
            captures.Add(capture);
        }

        var displayedRotation = captures[^1].Rendered;
        await Client.WaitPost(() =>
        {
            var combat = CEntMan.GetComponent<CombatModeComponent>(CPlayer);
            Assert.That(combat.IsInCombatMode, Is.True);
            Assert.That(combat.CombatToggleActionEntity, Is.Not.Null);
            var action = combat.CombatToggleActionEntity!.Value;
            var actionComponent = CEntMan.GetComponent<ActionComponent>(action);
            CEntMan.System<Content.Client.Actions.ActionsSystem>().TriggerAction((action, actionComponent));

            Assert.That(combat.IsInCombatMode, Is.False);
            Assert.That(CEntMan.HasComponent<MouseRotatorComponent>(CPlayer), Is.False);
        });

        for (var settle = 0; settle < 20; settle++)
        {
            var beforeState = await CaptureRotation(changes + settle, 0);
            captures.Add(beforeState);
            AssertRotationSnapped(displayedRotation, beforeState, captures);

            await Server.WaitRunTicks(1);
            var afterState = await CaptureRotation(changes + settle, 1);
            captures.Add(afterState);
            AssertRotationSnapped(displayedRotation, afterState, captures);

            await Client.WaitRunTicks(1);
            for (var frame = 2; frame <= 4; frame++)
            {
                var capture = await CaptureRotation(changes + settle, frame);
                captures.Add(capture);
                AssertRotationSnapped(displayedRotation, capture, captures);
            }
        }
    }

    [Test]
    public async Task CombatFacingSurvivesTransientInvalidMousePositionTest()
    {
        await SetCombatMode(true);
        await RunTicks(5);

        await Client.WaitAssertion(() =>
        {
            var transforms = CEntMan.System<TransformSystem>();
            var eye = Client.ResolveDependency<IEyeManager>();
            var player = transforms.GetRenderMapCoordinates(CPlayer);
            SetMouseScreenPosition(eye.MapToScreen(new MapCoordinates(player.Position - Vector2.UnitX, player.MapId)));

            var timing = Client.ResolveDependency<IClientGameTiming>();
            timing.InSimulation = false;
            try
            {
                CEntMan.FrameUpdate(1f / 144f);
                var displayedRotation = transforms.GetRenderWorldRotation(CPlayer);
                Assert.That(displayedRotation.EqualsApprox(transforms.GetWorldRotation(CPlayer)), Is.False);

                SetMouseScreenPosition(ScreenCoordinates.Invalid);
                CEntMan.FrameUpdate(1f / 144f);
                Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(displayedRotation), Is.True);
            }
            finally
            {
                timing.InSimulation = true;
            }
        });

        await SetCombatMode(false);
    }

    [Test]
    public async Task CombatMouseRotationSnapsWithoutPredictionCorrectionTest()
    {
        await OverrideCVar(Side.Client, CVars.NetPredictTickBias, 8);
        await SetCombatMode(true);
        await RunTicks(5);
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.HasComponent<MouseRotatorComponent>(CPlayer), Is.True));
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Down);

        for (var batch = 0; batch < 8; batch++)
        {
            await Server.WaitRunTicks(8);

            for (var batchTick = 0; batchTick < 8; batchTick++)
            {
                var tick = batch * 8 + batchTick;
                var theta = tick * MathF.Tau / 32f;
                var direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));
                var targetRotation = Angle.Zero;
                await Client.WaitPost(() =>
                {
                    var transforms = CEntMan.System<TransformSystem>();
                    var eye = Client.ResolveDependency<IEyeManager>();
                    var player = transforms.GetRenderMapCoordinates(CPlayer);
                    var mouse = eye.MapToScreen(new MapCoordinates(player.Position + direction * 0.25f, player.MapId));
                    var mappedMouse = eye.PixelToMap(mouse);
                    Assert.That(mappedMouse.MapId, Is.EqualTo(player.MapId));
                    Assert.That((mappedMouse.Position - player.Position - direction * 0.25f).LengthSquared(), Is.LessThan(0.000001f));
                    var eyeRotation = eye.CurrentEye.Rotation;
                    targetRotation = ((mappedMouse.Position - player.Position).ToWorldAngle() + eyeRotation)
                        .GetCardinalDir()
                        .ToAngle() - eyeRotation;
                    SetMouseScreenPosition(mouse);
                });

                await Client.WaitRunTicks(1);

                for (var frame = 1; frame <= 3; frame++)
                {
                    await Client.WaitAssertion(() =>
                    {
                        var timing = Client.ResolveDependency<IClientGameTiming>();
                        timing.TickRemainder = TimeSpan.FromTicks(timing.TickPeriod.Ticks * frame / 4);
                        CEntMan.FrameUpdate(1f / 144f);

                        var transforms = CEntMan.System<TransformSystem>();
                        var eye = Client.ResolveDependency<IEyeManager>();
                        var mappedMouse = eye.PixelToMap(InputManager.MouseScreenPosition);
                        var renderPosition = transforms.GetRenderMapCoordinates(CPlayer);
                        var eyeRotation = eye.CurrentEye.Rotation;
                        var displayedRotation = ((mappedMouse.Position - renderPosition.Position).ToWorldAngle() + eyeRotation)
                            .GetCardinalDir()
                            .ToAngle() - eyeRotation;
                        Assert.That(transforms.GetWorldRotation(CPlayer).EqualsApprox(targetRotation), Is.True,
                            $"simulation tick {tick}, frame {frame}");
                        Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(displayedRotation), Is.True,
                            $"displayed cursor tick {tick}, frame {frame}");

                        if (transforms.TryGetRenderPoseDebugData(CPlayer, out var data))
                        {
                            Assert.That(data.Type, Is.Not.EqualTo(RenderInterpolationType.PredictionCorrection),
                                $"correction type tick {tick}, frame {frame}: {data}");
                            Assert.That(data.CorrectionTranslation.LengthSquared(), Is.LessThan(0.0000001f),
                                $"translation tick {tick}, frame {frame}: {data}");
                            Assert.That(Math.Abs(data.CorrectionRotation.Theta), Is.LessThan(0.0001),
                                $"rotation tick {tick}, frame {frame}: {data}");
                        }
                    });
                }
            }
        }

        await SetMovementKey(DirectionFlag.East, BoundKeyState.Up);
        await RunTicks(3);
        await Client.WaitAssertion(() =>
        {
            SetMouseScreenPosition(ScreenCoordinates.Invalid);
            CEntMan.FrameUpdate(1f / 144f);
            var transforms = CEntMan.System<TransformSystem>();
            Assert.That(transforms.GetRenderWorldRotation(CPlayer).EqualsApprox(
                transforms.GetWorldRotation(CPlayer)), Is.True);
        });
    }

    private void SetMouseScreenPosition(ScreenCoordinates position)
    {
        var mousePosition = FindField(InputManager.GetType(), "_mouseScreenPosition");
        Assert.That(mousePosition, Is.Not.Null);
        mousePosition!.SetValue(InputManager, position);
    }

    private void SetMouseDirection(Vector2 direction)
    {
        var transforms = CEntMan.System<TransformSystem>();
        var eye = Client.ResolveDependency<IEyeManager>();
        var player = transforms.GetRenderMapCoordinates(CPlayer);
        SetMouseScreenPosition(eye.MapToScreen(new MapCoordinates(player.Position + direction, player.MapId)));
    }

    private static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) is { } field)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    private async Task<RotationCapture> CaptureRotation(int step, int frame)
    {
        var capture = default(RotationCapture);
        await Client.WaitPost(() =>
        {
            var timing = Client.ResolveDependency<IClientGameTiming>();
            timing.InSimulation = false;
            try
            {
                timing.TickRemainder = timing.TickPeriod * frame / 4;
                CEntMan.FrameUpdate(1f / 144f);

                var transforms = CEntMan.System<TransformSystem>();
                var simulation = transforms.GetWorldRotation(CPlayer);
                var rendered = transforms.GetRenderWorldRotation(CPlayer);
                RenderInterpolationType? type = null;
                var alpha = 1f;
                var correction = Angle.Zero;
                var correctionTranslation = Vector2.Zero;
                var source = simulation;
                var target = simulation;
                var simulationPosition = transforms.GetWorldPosition(CPlayer);
                var renderedPosition = transforms.GetRenderWorldPosition(CPlayer);
                var sourcePosition = simulationPosition;
                var targetPosition = simulationPosition;
                if (transforms.TryGetRenderPoseDebugData(CPlayer, out var data))
                {
                    type = data.Type;
                    alpha = data.Alpha;
                    correction = data.CorrectionRotation;
                    correctionTranslation = data.CorrectionTranslation;
                    source = data.Source.Rotation;
                    target = data.Target.Rotation;
                    sourcePosition = data.Source.Position;
                    targetPosition = data.Target.Position;
                }

                capture = new RotationCapture(
                    step,
                    frame,
                    timing.CurTick,
                    timing.TickPhase,
                    simulation,
                    rendered,
                    type,
                    alpha,
                    correction,
                    source,
                    target,
                    simulationPosition,
                    renderedPosition,
                    correctionTranslation,
                    sourcePosition,
                    targetPosition);
            }
            finally
            {
                timing.InSimulation = true;
            }
        });
        return capture;
    }

    private static void AssertRotationSnapped(
        Angle expected,
        RotationCapture capture,
        List<RotationCapture> captures)
    {
        var error = Math.Abs(Angle.ShortestDistance(expected, capture.Rendered).Degrees);
        if (error < 0.001)
            return;

        foreach (var row in captures)
            TestContext.Out.WriteLine(row);

        Assert.Fail($"Rendered rotation error {error} degrees: {capture}");
    }

    private static void AssertOrdinaryMovement(RotationCapture capture, List<RotationCapture> captures)
    {
        var rotationError = Math.Abs(Angle.ShortestDistance(capture.Simulation, capture.Rendered).Degrees);
        var translationCorrection = capture.CorrectionTranslation.LengthSquared();
        var rotationCorrection = Math.Abs(capture.Correction.Degrees);
        var movedBackwards = captures.Count > 1 &&
                             capture.RenderedPosition.X + 0.0001f < captures[^2].RenderedPosition.X;

        if (rotationError < 0.001 &&
            translationCorrection < 0.0000001f &&
            rotationCorrection < 0.001 &&
            !movedBackwards)
        {
            return;
        }

        foreach (var row in captures)
            TestContext.Out.WriteLine(row);

        Assert.Multiple(() =>
        {
            Assert.That(rotationError, Is.LessThan(0.001), $"rendered rotation diverged: {capture}");
            Assert.That(translationCorrection, Is.LessThan(0.0000001f), $"translation correction appeared: {capture}");
            Assert.That(rotationCorrection, Is.LessThan(0.001), $"rotation correction appeared: {capture}");
            Assert.That(movedBackwards, Is.False, $"rendered movement reversed: {capture}");
        });
    }

    private readonly record struct RotationCapture(
        int Step,
        int Frame,
        GameTick Tick,
        float Phase,
        Angle Simulation,
        Angle Rendered,
        RenderInterpolationType? Type,
        float Alpha,
        Angle Correction,
        Angle Source,
        Angle Target,
        Vector2 SimulationPosition,
        Vector2 RenderedPosition,
        Vector2 CorrectionTranslation,
        Vector2 SourcePosition,
        Vector2 TargetPosition)
    {
        public override string ToString()
        {
            return $"step={Step},frame={Frame},tick={Tick.Value},phase={Phase:R}," +
                   $"simulation={Simulation.Degrees:R},rendered={Rendered.Degrees:R}," +
                   $"type={Type?.ToString() ?? "None"},alpha={Alpha:R},correction={Correction.Degrees:R}," +
                   $"source={Source.Degrees:R},target={Target.Degrees:R}," +
                   $"simulationPosition={SimulationPosition},renderedPosition={RenderedPosition}," +
                   $"correctionTranslation={CorrectionTranslation},sourcePosition={SourcePosition}," +
                   $"targetPosition={TargetPosition}";
        }
    }
}
