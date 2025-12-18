using System.Linq;
using Content.Server.Database;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(PardonCommand))]
    public sealed class PardonCommand
    {
        private static readonly TimeSpan MarginOfError = TimeSpan.FromMinutes(1);

        [Test]
        public async Task PardonTest()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var server = pair.Server;
            var client = pair.Client;

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sConsole = server.ResolveDependency<IServerConsoleHost>();
            var sDatabase = server.ResolveDependency<IServerDbManager>();
            var netMan = client.ResolveDependency<IClientNetManager>();
            var clientSession = sPlayerManager.Sessions.Single();
            var clientId = clientSession.UserId;

            Assert.That(netMan.IsConnected);

            Assert.That(sPlayerManager.Sessions, Has.Length.EqualTo(1));
            // No bans on record
            Assert.Multiple(async () =>
            {
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Is.Empty);
            });

            // Try to pardon a ban that does not exist
            await server.WaitPost(() => sConsole.ExecuteCommand("pardon 1"));

            // Still no bans on record
            Assert.Multiple(async () =>
            {
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Is.Empty);
            });

            var banReason = "test";

            Assert.That(sPlayerManager.Sessions, Has.Length.EqualTo(1));
            // Ban the client for 24 hours
            await server.WaitPost(() => sConsole.ExecuteCommand($"ban {clientSession.Name} {banReason} 1440"));

            // Should have one ban on record now
            Assert.Multiple(async () =>
            {
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Not.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Not.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));
            });

            await pair.RunTicksSync(5);
            Assert.That(sPlayerManager.Sessions, Has.Length.EqualTo(0));
            Assert.That(!netMan.IsConnected);

            // Try to pardon a ban that does not exist
            await server.WaitPost(() => sConsole.ExecuteCommand("pardon 2"));

            // The existing ban is unaffected
            Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Not.Null);

            var ban = await sDatabase.GetServerBanAsync(1);
            Assert.Multiple(async () =>
            {
                Assert.That(ban, Is.Not.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));

                // Check that it matches
                Assert.That(ban.Id, Is.EqualTo(1));
                Assert.That(ban.UserId, Is.EqualTo(clientId));
                Assert.That(ban.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(ban.ExpirationTime, Is.Not.Null);
                Assert.That(ban.ExpirationTime.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(ban.Reason, Is.EqualTo(banReason));

                // Done through the console
                Assert.That(ban.BanningAdmin, Is.Null);
                Assert.That(ban.Unban, Is.Null);
            });

            // Pardon the actual ban
            await server.WaitPost(() => sConsole.ExecuteCommand("pardon 1"));

            // No bans should be returned
            Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Null);

            // Direct id lookup returns a pardoned ban
            var pardonedBan = await sDatabase.GetServerBanAsync(1);
            Assert.Multiple(async () =>
            {
                // Check that it matches
                Assert.That(pardonedBan, Is.Not.Null);

                // The list is still returned since that ignores pardons
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));

                Assert.That(pardonedBan.Id, Is.EqualTo(1));
                Assert.That(pardonedBan.UserId, Is.EqualTo(clientId));
                Assert.That(pardonedBan.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(pardonedBan.ExpirationTime, Is.Not.Null);
                Assert.That(pardonedBan.ExpirationTime.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(pardonedBan.Reason, Is.EqualTo(banReason));

                // Done through the console
                Assert.That(pardonedBan.BanningAdmin, Is.Null);

                Assert.That(pardonedBan.Unban, Is.Not.Null);
                Assert.That(pardonedBan.Unban.BanId, Is.EqualTo(1));

                // Done through the console
                Assert.That(pardonedBan.Unban.UnbanningAdmin, Is.Null);

                Assert.That(pardonedBan.Unban.UnbanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
            });

            // Try to pardon it again
            await server.WaitPost(() => sConsole.ExecuteCommand("pardon 1"));

            // Nothing changes
            Assert.Multiple(async () =>
            {
                // No bans should be returned
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null, null), Is.Null);

                // Direct id lookup returns a pardoned ban
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Not.Null);

                // The list is still returned since that ignores pardons
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));
            });

            // Reconnect client. Slightly faster than dirtying the pair.
            Assert.That(sPlayerManager.Sessions, Is.Empty);
            client.SetConnectTarget(server);
            await client.WaitPost(() => netMan.ClientConnect(null!, 0, null!));
            await pair.RunTicksSync(5);
            Assert.That(sPlayerManager.Sessions, Has.Length.EqualTo(1));

            await pair.CleanReturnAsync();
        }
    }
}
