using System.Numerics;
using Content.Client.Clickable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ClickableTest
    {
        private const double DirSouth = 0;
        private const double DirNorth = Math.PI;
        private const double DirEast = Math.PI / 2;
        private const double DirSouthEast = Math.PI / 4;
        private const double DirSouthEastJustShy = Math.PI / 4 - 0.1;

        [Test]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        public async Task<bool> Test(string prototype, float clickPosX, float clickPosY, double angle, float scale)
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pair.Server;
            var client = pair.Client;

            var clientEntManager = client.ResolveDependency<IEntityManager>();
            var serverEntManager = server.ResolveDependency<IEntityManager>();
            var eyeManager = client.ResolveDependency<IEyeManager>();
            var spriteQuery = clientEntManager.GetEntityQuery<SpriteComponent>();
            var eye = client.ResolveDependency<IEyeManager>().CurrentEye;

            var testMap = await pair.CreateTestMap();

            EntityUid serverEnt = default;

            await server.WaitPost(() =>
            {
                serverEnt = serverEntManager.SpawnEntity(prototype, testMap.GridCoords);
                serverEntManager.System<SharedTransformSystem>().SetWorldRotation(serverEnt, angle);
            });

            // Let client sync up.
            await pair.RunTicksSync(5);

            var hit = false;
            var clientEnt = clientEntManager.GetEntity(serverEntManager.GetNetEntity(serverEnt));

            await client.WaitPost(() =>
            {
                var sprite = spriteQuery.GetComponent(clientEnt);
                sprite.Scale = new Vector2(scale, scale);

                // these tests currently all assume player eye is 0
                eyeManager.CurrentEye.Rotation = 0;

                var pos = clientEntManager.System<SharedTransformSystem>().GetWorldPosition(clientEnt);

                hit = clientEntManager.System<ClickableSystem>().CheckClick((clientEnt, null, sprite, null), new Vector2(clickPosX, clickPosY) + pos, eye, out _, out _, out _);
            });

            await server.WaitPost(() =>
            {
                serverEntManager.DeleteEntity(serverEnt);
            });

            await pair.CleanReturnAsync();

            return hit;
        }
    }
}
