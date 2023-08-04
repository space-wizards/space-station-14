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
    [TestOf(typeof(RoundRestartCleanupEvent))]
    public sealed class ResettingEntitySystemTests
    {
        [Reflect(false)]
        public sealed class TestRoundRestartCleanupEvent : EntitySystem
        {
            public bool HasBeenReset { get; set; }

            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            }

            public void Reset(RoundRestartCleanupEvent ev)
            {
                HasBeenReset = true;
            }
        }

        [Test]
        public async Task ResettingEntitySystemResetTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var gameTicker = entitySystemManager.GetEntitySystem<GameTicker>();

            await server.WaitAssertion(() =>
            {
                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                var system = entitySystemManager.GetEntitySystem<TestRoundRestartCleanupEvent>();

                system.HasBeenReset = false;

                Assert.False(system.HasBeenReset);

                gameTicker.RestartRound();

                Assert.True(system.HasBeenReset);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
