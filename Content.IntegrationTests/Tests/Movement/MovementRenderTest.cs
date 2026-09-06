using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

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
}
