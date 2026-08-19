#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Database;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Commands;

[TestOf(typeof(PardonCommand))]
public sealed class PardonCommand : GameTest
{
    private static readonly TimeSpan MarginOfError = TimeSpan.FromMinutes(1);

    [SidedDependency(Side.Server)] private IPlayerManager _sPlayerManager = default!;
    [SidedDependency(Side.Server)] private IServerConsoleHost _sConsole = default!;
    [SidedDependency(Side.Server)] private IServerDbManager _sDatabaseManager = default!;
    [SidedDependency(Side.Client)] private IClientNetManager _cNetManager = default!;

    [Test]
    public async Task PardonTest()
    {
        var clientSession = _sPlayerManager.Sessions.Single();
        var clientId = clientSession.UserId;

        Assert.That(_cNetManager.IsConnected);

        Assert.That(_sPlayerManager.Sessions, Has.Length.EqualTo(1));
        // No bans on record
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Null);
            Assert.That(await _sDatabaseManager.GetBanAsync(1), Is.Null);
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Is.Empty);
        }

        // Try to pardon a ban that does not exist
        await Pair.WaitCommand("pardon 1");

        // Still no bans on record
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Null);
            Assert.That(await _sDatabaseManager.GetBanAsync(1), Is.Null);
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Is.Empty);
        }

        const string banReason = "test";

        Assert.That(_sPlayerManager.Sessions, Has.Length.EqualTo(1));
        // Ban the client for 24 hours
        await Pair.WaitCommand($"ban {clientSession.Name} {banReason} 1440");

        // Should have one ban on record now
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Not.Null);
            Assert.That(await _sDatabaseManager.GetBanAsync(1), Is.Not.Null);
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));
        }

        await RunTicksSync(5);
        Assert.That(_sPlayerManager.Sessions, Has.Length.EqualTo(0));
        Assert.That(!_cNetManager.IsConnected);

        // Try to pardon a ban that does not exist
        await Pair.WaitCommand("pardon 2");

        // The existing ban is unaffected
        Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Not.Null);

        var ban = await _sDatabaseManager.GetBanAsync(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ban, Is.Not.Null);
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));

            // Check that it matches
            Assert.That(ban.Id, Is.EqualTo(1));
            Assert.That(ban.UserIds, Is.EquivalentTo([clientId]));
            Assert.That(ban.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
            Assert.That(ban.ExpirationTime, Is.Not.Null);
            Assert.That(ban.ExpirationTime!.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
            Assert.That(ban.Reason, Is.EqualTo(banReason));

            // Done through the console
            Assert.That(ban.BanningAdmin, Is.Null);
            Assert.That(ban.Unban, Is.Null);
        }

        // Pardon the actual ban
        await Pair.WaitCommand("pardon 1");

        // No bans should be returned
        Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Null);

        // Direct id lookup returns a pardoned ban
        var pardonedBan = await _sDatabaseManager.GetBanAsync(1);
        using (Assert.EnterMultipleScope())
        {
            // Check that it matches
            Assert.That(pardonedBan, Is.Not.Null);

            // The list is still returned since that ignores pardons
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));

            Assert.That(pardonedBan!.Id, Is.EqualTo(1));
            Assert.That(pardonedBan.UserIds, Is.EquivalentTo([clientId]));
            Assert.That(pardonedBan.BanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
            Assert.That(pardonedBan.ExpirationTime, Is.Not.Null);
            Assert.That(pardonedBan.ExpirationTime!.Value.UtcDateTime - DateTime.UtcNow.AddHours(24), Is.LessThanOrEqualTo(MarginOfError));
            Assert.That(pardonedBan.Reason, Is.EqualTo(banReason));

            // Done through the console
            Assert.That(pardonedBan.BanningAdmin, Is.Null);

            Assert.That(pardonedBan.Unban, Is.Not.Null);
            Assert.That(pardonedBan.Unban!.BanId, Is.EqualTo(1));

            // Done through the console
            Assert.That(pardonedBan.Unban.UnbanningAdmin, Is.Null);

            Assert.That(pardonedBan.Unban.UnbanTime.UtcDateTime - DateTime.UtcNow, Is.LessThanOrEqualTo(MarginOfError));
        }

        // Try to pardon it again
        await Pair.WaitCommand("pardon 1");

        // Nothing changes
        using (Assert.EnterMultipleScope())
        {
            // No bans should be returned
            Assert.That(await _sDatabaseManager.GetBanAsync(null, clientId, null, null), Is.Null);

            // Direct id lookup returns a pardoned ban
            Assert.That(await _sDatabaseManager.GetBanAsync(1), Is.Not.Null);

            // The list is still returned since that ignores pardons
            Assert.That(await _sDatabaseManager.GetBansAsync(null, clientId, null, null), Has.Count.EqualTo(1));
        }

        // Reconnect client. Slightly faster than dirtying the pair.
        Assert.That(_sPlayerManager.Sessions, Is.Empty);
        await Pair.Connect();
        Assert.That(_sPlayerManager.Sessions, Has.Length.EqualTo(1));
    }
}
