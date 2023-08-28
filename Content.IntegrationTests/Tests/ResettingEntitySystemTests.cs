using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
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
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Connected = true
            });
            var server = pair.Server;

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var gameTicker = entitySystemManager.GetEntitySystem<GameTicker>();

            await server.WaitAssertion(() =>
            {
                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                var system = entitySystemManager.GetEntitySystem<TestRoundRestartCleanupEvent>();

                system.HasBeenReset = false;

                Assert.That(system.HasBeenReset, Is.False);

                gameTicker.RestartRound();

                Assert.That(system.HasBeenReset);
            });
            await pair.CleanReturnAsync();
        }
    }
}
