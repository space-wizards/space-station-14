using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Replays;

namespace Content.IntegrationTests.Tests.Replays;

[TestFixture]
public sealed class ReplayTests
{
    /// <summary>
    /// Simple test that just makes sure that automatic replay recording on round restarts works without any issues.
    /// </summary>
    [Test]
    public async Task AutoRecordReplayTest()
    {
        var settings = new PoolSettings {DummyTicker = false};
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;

        Assert.That(server.CfgMan.GetCVar(CVars.ReplayServerRecordingEnabled), Is.False);
        var recordMan = server.ResolveDependency<IReplayRecordingManager>();
        Assert.That(recordMan.IsRecording, Is.False);

        // Setup cvars.
        var autoRec = server.CfgMan.GetCVar(CCVars.ReplayAutoRecord);
        var autoRecName = server.CfgMan.GetCVar(CCVars.ReplayAutoRecordName);
        var tempDir = server.CfgMan.GetCVar(CCVars.ReplayAutoRecordTempDir);
        server.CfgMan.SetCVar(CVars.ReplayServerRecordingEnabled, true);
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecord, true);
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecordTempDir, "/a/b/");
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecordName, $"c/d/{autoRecName}");

        // Restart the round a few times
        var ticker = server.System<GameTicker>();
        await server.WaitPost(() => ticker.RestartRound());
        await pair.RunTicksSync(25);
        Assert.That(recordMan.IsRecording, Is.True);
        await server.WaitPost(() => ticker.RestartRound());
        await pair.RunTicksSync(25);
        Assert.That(recordMan.IsRecording, Is.True);

        // Reset cvars
        server.CfgMan.SetCVar(CVars.ReplayServerRecordingEnabled, false);
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecord, autoRec);
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecordTempDir, tempDir);
        server.CfgMan.SetCVar(CCVars.ReplayAutoRecordName, autoRecName);

        // Restart the round again to disable the current recording.
        await server.WaitPost(() => ticker.RestartRound());
        await pair.RunTicksSync(25);
        Assert.That(recordMan.IsRecording, Is.False);

        await pair.CleanReturnAsync();
    }
}
