#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.AuditLog;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests.Administration.Logs;

/// <summary>
/// Tests for the bug where admin logs created in the lobby after a completed
/// round were permanently attributed to the previous round's ID instead of being buffered
/// as pre-round logs for the next round.
///
/// Covered scenarios:
/// - First lobby → round 1: pre-round logs get round 1's ID (baseline)
/// - Round 1 → lobby → round 2: lobby logs must NOT carry round 1's ID; they must carry round 2's ID
/// - Multiple lobby logs between rounds all get the next round's ID
/// - In-round logs must remain attributed to round 1 after the transition
/// </summary>
[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class AdminLogRoundTransitionTests : GameTest
{
    // Fresh = true so we start from a clean server instance with no leftover state from
    // a previous test's round. Dirty = true because we cycle run levels during the test.
    public override PoolSettings PoolSettings => new()
    {
        Fresh = true,
        Dirty = true,
        InLobby = true,
        AdminLogsEnabled = true,
    };

    [SidedDependency(Side.Server)] private readonly IAdminLogManager _sAdminLogManager = null!;
    [SidedDependency(Side.Server)] private readonly GameTicker _sGameTicker = null!;

    /// <summary>
    /// Logs created in the lobby before the very first round are attributed to
    /// that round once it starts. The pre-round queue must be drained and the resulting
    /// persisted logs must carry the correct round ID.
    /// </summary>
    [Test]
    public async Task PreRoundLogsGetFirstRoundId()
    {
        var guid = Guid.NewGuid();

        // Create a log while still in lobby (before any round has started).
        await Server.WaitPost(() =>
        {
            _sAdminLogManager.Add(LogType.Unknown, $"pre-round log: {guid}");
        });

        // Start round 1.
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);

        Assert.That(round1Id, Is.GreaterThan(0), "Round 1 ID should be a valid positive integer");

        // Wait until the pre-round log is flushed and stored with round 1's ID.
        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sAdminLogManager.All(new LogFilter
            {
                Round = round1Id,
                Search = guid.ToString(),
            });
            return logs.Count > 0;
        });

        var logs = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = guid.ToString() });
        Assert.That(logs, Has.Count.EqualTo(1),
            "Pre-round log should be attributed to round 1 (found via round filter)");
    }

    /// <summary>
    /// Logs created in the lobby AFTER round 1 has ended must NOT carry
    /// round 1's ID. They should be buffered as pre-round and receive round 2's ID.
    /// </summary>
    [Test]
    public async Task LobbyLogsAfterRound1DoNotCarryRound1Id()
    {
        // Round 1
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);
        Assert.That(round1Id, Is.GreaterThan(0));

        var inRoundGuid = Guid.NewGuid();
        await Server.WaitPost(() =>
        {
            _sAdminLogManager.Add(LogType.Unknown, $"in-round-1 log: {inRoundGuid}");
        });

        // Return to lobby
        await Server.WaitPost(() =>
        {
            _sGameTicker.EndRound();
            _sGameTicker.RestartRound();
        });

        // Lobby log between round 1 and round 2
        var lobbyGuid = Guid.NewGuid();
        await Server.WaitPost(() =>
        {
            _sAdminLogManager.Add(LogType.Unknown, $"inter-round lobby log: {lobbyGuid}");
        });

        // Round 2
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round2Id = 0;
        await Server.WaitPost(() => round2Id = _sGameTicker.RoundId);
        Assert.That(round2Id, Is.GreaterThan(round1Id), "Round 2 ID must be greater than round 1 ID");

        // Wait for the lobby log to appear under round 2
        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sAdminLogManager.All(new LogFilter
            {
                Round = round2Id,
                Search = lobbyGuid.ToString(),
            });
            return logs.Count > 0;
        });

        // Lobby log lives under round 2, not round 1
        var underRound2 = await _sAdminLogManager.All(new LogFilter { Round = round2Id, Search = lobbyGuid.ToString() });
        Assert.That(underRound2, Has.Count.EqualTo(1),
            "Lobby log must be retrievable via round 2's filter");

        var underRound1 = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = lobbyGuid.ToString() });
        Assert.That(underRound1, Is.Empty,
            "Lobby log must not appear under round 1's filter");

        // Also verify the in-round log still belongs to round 1
        await PoolManager.WaitUntil(Server, async () =>
        {
            var r1Logs = await _sAdminLogManager.All(new LogFilter
            {
                Round = round1Id,
                Search = inRoundGuid.ToString(),
            });
            return r1Logs.Count > 0;
        });

        var r1InRoundLogs = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = inRoundGuid.ToString() });
        Assert.That(r1InRoundLogs, Has.Count.EqualTo(1),
            "In-round log must remain attributed to round 1");
    }

    /// <summary>
    /// Multiple lobby logs created between round 1 and round 2 must all receive round 2's ID,
    /// not round 1's.
    /// </summary>
    [Test]
    public async Task MultipleLobbyLogsAllGetNextRoundId()
    {
        // Round 1
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);

        // Return to lobby
        await Server.WaitPost(() =>
        {
            _sGameTicker.EndRound();
            _sGameTicker.RestartRound();
        });

        // Several lobby logs
        var commonGuid = Guid.NewGuid();
        const int lobbyLogCount = 5;
        await Server.WaitPost(() =>
        {
            for (var i = 0; i < lobbyLogCount; i++)
                _sAdminLogManager.Add(LogType.Unknown, $"multi-lobby log {i}: {commonGuid}");
        });

        // Round 2
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round2Id = 0;
        await Server.WaitPost(() => round2Id = _sGameTicker.RoundId);

        // Wait for all lobby logs to be persisted under round 2.
        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sAdminLogManager.All(new LogFilter
            {
                Round = round2Id,
                Search = commonGuid.ToString(),
            });
            return logs.Count >= lobbyLogCount;
        });

        var underRound2 = await _sAdminLogManager.All(new LogFilter { Round = round2Id, Search = commonGuid.ToString() });
        var underRound1 = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = commonGuid.ToString() });

        Assert.That(underRound2, Has.Count.EqualTo(lobbyLogCount),
            "All lobby logs should appear under round 2");
        Assert.That(underRound1, Is.Empty,
            "No lobby log should appear under round 1");
    }

    /// <summary>
    /// Round 1 logs that are still in the queue when the round transitions to lobby must not
    /// have their round ID overwritten by the next round.
    /// </summary>
    [Test]
    public async Task Round1LogsStayAttributedAfterTransition()
    {
        // Round 1
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);

        var guid = Guid.NewGuid();
        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 3; i++)
                _sAdminLogManager.Add(LogType.Unknown, $"r1-log {i}: {guid}");
        });

        // Return to lobby then round 2
        await Server.WaitPost(() =>
        {
            _sGameTicker.EndRound();
            _sGameTicker.RestartRound();
        });

        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round2Id = 0;
        await Server.WaitPost(() => round2Id = _sGameTicker.RoundId);

        // Wait for round 1 logs to appear.
        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = guid.ToString() });
            return logs.Count >= 3;
        });

        var r1Logs = await _sAdminLogManager.All(new LogFilter { Round = round1Id, Search = guid.ToString() });
        var r2Logs = await _sAdminLogManager.All(new LogFilter { Round = round2Id, Search = guid.ToString() });

        Assert.That(r1Logs, Has.Count.EqualTo(3),
            "All round 1 logs must remain attributed to round 1 even if flushed after the transition");
        Assert.That(r2Logs, Is.Empty,
            "Round 1 logs must not be accidentally attributed to round 2");
    }
}

/// <summary>
/// Tests for <see cref="AdminAuditLogManager"/> round-state tracking across
/// run-level transitions. Mirrors the admin-log tests above to ensure the two subsystems
/// behave consistently.
/// </summary>
[TestFixture]
[TestOf(typeof(AdminAuditLogSystem))]
public sealed class AdminAuditLogRoundTransitionTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Fresh = true,
        Dirty = true,
        InLobby = true,
        AdminLogsEnabled = true,
    };

    [SidedDependency(Side.Server)] private readonly IAdminAuditLogManager _sAuditLogManager = null!;
    [SidedDependency(Side.Server)] private readonly IServerDbManager _sDbManager = null!;
    [SidedDependency(Side.Server)] private readonly GameTicker _sGameTicker = null!;

    private static readonly Guid TestAdminGuid = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    // Wait until a round-scoped audit log with the given message appears.
    private async Task WaitForAuditLog(int roundId, string message)
    {
        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await _sDbManager.GetAuditLogs(new AuditLogFilter { Round = roundId });
            return logs.Any(l => l.Message == message);
        });
    }

    // Assert an audit log with the given message appears under the given round.
    private async Task<bool> AuditLogExistsForRound(int roundId, string message)
    {
        var logs = await _sDbManager.GetAuditLogs(new AuditLogFilter { Round = roundId });
        return logs.Any(l => l.Message == message);
    }

    /// <summary>
    /// Baseline: audit logs created in the lobby before any round are buffered as pre-round
    /// and attributed to round 1 when it starts.
    /// </summary>
    [Test]
    public async Task PreRoundAuditLogsGetFirstRoundId()
    {
        var message = $"pre-round audit: {Guid.NewGuid()}";

        await Server.WaitPost(() =>
        {
            _sAuditLogManager.LogAction(
                TestAdminGuid,
                AdminAuditAction.CommandExecution,
                AuditSeverity.Routine,
                message);
        });

        // Start round 1.
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);
        Assert.That(round1Id, Is.GreaterThan(0));

        await WaitForAuditLog(round1Id, message);

        Assert.That(await AuditLogExistsForRound(round1Id, message), Is.True,
            "Pre-round audit log should be attributable to round 1");
    }

    /// <summary>
    /// Core regression for audit logs: after round 1 ends and the server returns to lobby,
    /// new audit logs must NOT carry round 1's ID. They should be buffered and receive
    /// round 2's ID.
    /// </summary>
    [Test]
    public async Task LobbyAuditLogsAfterRound1DoNotCarryRound1Id()
    {
        // Round 1
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round1Id = 0;
        await Server.WaitPost(() => round1Id = _sGameTicker.RoundId);
        Assert.That(round1Id, Is.GreaterThan(0));

        var inRoundMessage = $"in-round-1 audit: {Guid.NewGuid()}";
        await Server.WaitPost(() =>
        {
            _sAuditLogManager.LogAction(
                TestAdminGuid,
                AdminAuditAction.CommandExecution,
                AuditSeverity.Routine,
                inRoundMessage);
        });

        // Return to lobby
        await Server.WaitPost(() =>
        {
            _sGameTicker.EndRound();
            _sGameTicker.RestartRound();
        });

        // Lobby audit log
        var lobbyMessage = $"inter-round lobby audit: {Guid.NewGuid()}";
        await Server.WaitPost(() =>
        {
            _sAuditLogManager.LogAction(
                TestAdminGuid,
                AdminAuditAction.CommandExecution,
                AuditSeverity.Routine,
                lobbyMessage);
        });

        // Round 2
        await Server.WaitPost(() => _sGameTicker.StartRound(force: true));

        var round2Id = 0;
        await Server.WaitPost(() => round2Id = _sGameTicker.RoundId);
        Assert.That(round2Id, Is.GreaterThan(round1Id));

        // Wait for the lobby audit log to appear under round 2.
        await WaitForAuditLog(round2Id, lobbyMessage);

        // Assertions
        Assert.That(await AuditLogExistsForRound(round2Id, lobbyMessage), Is.True,
            "Lobby audit log must be attributable to round 2");
        Assert.That(await AuditLogExistsForRound(round1Id, lobbyMessage), Is.False,
            "Lobby audit log must not appear under round 1's filter");

        // In-round log must stay with round 1.
        await WaitForAuditLog(round1Id, inRoundMessage);
        Assert.That(await AuditLogExistsForRound(round1Id, inRoundMessage), Is.True,
            "In-round audit log must remain attributed to round 1");
    }
}
