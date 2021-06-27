using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(IResettingEntitySystem))]
    public class ResettingEntitySystemTests : ContentIntegrationTest
    {
        [Reflect(false)]
        private class TestResettingEntitySystem : EntitySystem, IResettingEntitySystem
        {
            public bool HasBeenReset { get; set; }

            public void Reset()
            {
                HasBeenReset = true;
            }
        }

        [Test]
        public async Task ResettingEntitySystemResetTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestResettingEntitySystem>();
                }
            });

            await server.WaitIdleAsync();

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var gameTicker = entitySystemManager.GetEntitySystem<GameTicker>();

            await server.WaitAssertion(() =>
            {
                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                var system = entitySystemManager.GetEntitySystem<TestResettingEntitySystem>();

                system.HasBeenReset = false;

                Assert.False(system.HasBeenReset);

                gameTicker.RestartRound();

                Assert.True(system.HasBeenReset);
            });
        }
    }
}
