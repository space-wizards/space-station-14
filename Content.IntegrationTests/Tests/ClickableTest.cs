using System;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ClickableTest : ContentIntegrationTest
    {
        private ClientIntegrationInstance _client;
        private ServerIntegrationInstance _server;

        private const double DirSouth = 0;
        private const double DirNorth = Math.PI;
        private const double DirEast = Math.PI / 2;
        private const double DirSouthEast = Math.PI / 4;
        private const double DirSouthEastJustShy = Math.PI / 4 - 0.1;

        [OneTimeSetUp]
        public async Task Setup()
        {
            (_client, _server) = await StartConnectedServerClientPair();
        }

        [Parallelizable(ParallelScope.None)]
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
            EntityUid entity = default;

            await _server.WaitPost(() =>
            {
                var entMgr = IoCManager.Resolve<IEntityManager>();
                var ent = entMgr.SpawnEntity(prototype, new MapCoordinates(0, 0, new MapId(1)));
                ent.Transform.LocalRotation = angle;
                ent.GetComponent<SpriteComponent>().Scale = (scale, scale);
                entity = ent.Uid;
            });

            // Let client sync up.
            await RunTicksSync(_client, _server, 5);

            var hit = false;

            await _client.WaitPost(() =>
            {
                var entMgr = IoCManager.Resolve<IEntityManager>();
                var ent = entMgr.GetEntity(entity);
                var clickable = ent.GetComponent<ClickableComponent>();

                hit = clickable.CheckClick((clickPosX, clickPosY), out _, out _);
            });

            await _server.WaitPost(() =>
            {
                var entMgr = IoCManager.Resolve<IEntityManager>();
                entMgr.DeleteEntity(entity);
            });

            return hit;
        }
    }
}
