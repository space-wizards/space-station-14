using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using NUnit.Framework;
using Robust.Server.Console;
using Robust.Server.Player;

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
            await using var pairTracker = await PoolManager.GetServerClient(new (){Destructive = true});
            var server = pairTracker.Pair.Server;

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sConsole = server.ResolveDependency<IServerConsoleHost>();
            var sDatabase = server.ResolveDependency<IServerDbManager>();

            await server.WaitAssertion(async () =>
            {
                var clientSession = sPlayerManager.Sessions.Single();
                var clientId = clientSession.UserId;

                // No bans on record
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Is.Empty);

                // Try to pardon a ban that does not exist
                sConsole.ExecuteCommand("pardon 1");

                // Still no bans on record
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Is.Empty);

                var banReason = "test";

                // Ban the client for 24 hours
                sConsole.ExecuteCommand($"ban {clientSession.Name} {banReason} 1440");

                // Should have one ban on record now
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Not.Null);
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Not.Null);
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Has.Count.EqualTo(1));

                // Try to pardon a ban that does not exist
                sConsole.ExecuteCommand("pardon 2");

                // The existing ban is unaffected
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Not.Null);

                var ban = await sDatabase.GetServerBanAsync(1);
                Assert.That(ban, Is.Not.Null);

                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Has.Count.EqualTo(1));

                // Check that it matches
                Assert.That(ban.Id, Is.EqualTo(1));
                Assert.That(ban.UserId, Is.EqualTo(clientId));
                Assert.That(ban.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
                Assert.NotNull(ban.ExpirationTime);
                Assert.That(ban.ExpirationTime.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(ban.Reason, Is.EqualTo(banReason));

                // Done through the console
                Assert.That(ban.BanningAdmin, Is.Null);

                Assert.That(ban.Unban, Is.Null);

                // Pardon the actual ban
                sConsole.ExecuteCommand("pardon 1");

                // No bans should be returned
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Null);

                // Direct id lookup returns a pardoned ban
                var pardonedBan = await sDatabase.GetServerBanAsync(1);
                Assert.That(pardonedBan, Is.Not.Null);

                // The list is still returned since that ignores pardons
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Has.Count.EqualTo(1));

                // Check that it matches
                Assert.That(pardonedBan.Id, Is.EqualTo(1));
                Assert.That(pardonedBan.UserId, Is.EqualTo(clientId));
                Assert.That(pardonedBan.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
                Assert.NotNull(pardonedBan.ExpirationTime);
                Assert.That(pardonedBan.ExpirationTime.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
                Assert.That(pardonedBan.Reason, Is.EqualTo(banReason));

                // Done through the console
                Assert.That(pardonedBan.BanningAdmin, Is.Null);

                Assert.That(pardonedBan.Unban, Is.Not.Null);
                Assert.That(pardonedBan.Unban.BanId, Is.EqualTo(1));

                // Done through the console
                Assert.That(pardonedBan.Unban.UnbanningAdmin, Is.Null);

                Assert.That(pardonedBan.Unban.UnbanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));

                // Try to pardon it again
                sConsole.ExecuteCommand("pardon 1");

                // Nothing changes
                // No bans should be returned
                Assert.That(await sDatabase.GetServerBanAsync(null, clientId, null), Is.Null);

                // Direct id lookup returns a pardoned ban
                Assert.That(await sDatabase.GetServerBanAsync(1), Is.Not.Null);

                // The list is still returned since that ignores pardons
                Assert.That(await sDatabase.GetServerBansAsync(null, clientId, null), Has.Count.EqualTo(1));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
