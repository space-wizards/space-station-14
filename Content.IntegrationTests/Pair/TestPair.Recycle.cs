#nullable enable
using System.IO;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Pair;

// This partial class contains logic related to recycling & disposing test pairs.
public sealed partial class TestPair
{
    protected override async Task Cleanup()
    {
        await base.Cleanup();
        await ResetModifiedPreferences();
    }

    private async Task ResetModifiedPreferences()
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        foreach (var user in _modifiedProfiles)
        {
            await Server.WaitPost(() => prefMan.SetProfile(user, 0, new HumanoidCharacterProfile()).Wait());
        }

        _modifiedProfiles.Clear();
    }

    protected override async Task Recycle(PairSettings next, TextWriter testOut)
    {
        // Move to pre-round lobby. Required to toggle dummy ticker on and off
        var gameTicker = Server.System<GameTicker>();
        if (gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Restarting round.");
            Server.CfgMan.SetCVar(CCVars.GameDummyTicker, false);
            Assert.That(gameTicker.DummyTicker, Is.False);
            Server.CfgMan.SetCVar(CCVars.GameLobbyEnabled, true);
            await Server.WaitPost(() => gameTicker.RestartRound());
            await RunTicksSync(1);
        }

        //Apply Cvars
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        await ApplySettings(next);
        await RunTicksSync(1);

        // Restart server.
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Restarting server again");
        await Server.WaitPost(() => Server.EntMan.FlushEntities());
        await Server.WaitPost(() => gameTicker.RestartRound());
        await RunTicksSync(1);
    }

    public override void ValidateSettings(PairSettings s)
    {
        base.ValidateSettings(s);
        var settings = (PoolSettings) s;

        var cfg = Server.CfgMan;
        Assert.That(cfg.GetCVar(CCVars.AdminLogsEnabled), Is.EqualTo(settings.AdminLogsEnabled));
        Assert.That(cfg.GetCVar(CCVars.GameLobbyEnabled), Is.EqualTo(settings.InLobby));
        Assert.That(cfg.GetCVar(CCVars.GameDummyTicker), Is.EqualTo(settings.DummyTicker));

        var ticker = Server.System<GameTicker>();
        Assert.That(ticker.DummyTicker, Is.EqualTo(settings.DummyTicker));

        var expectPreRound = settings.InLobby | settings.DummyTicker;
        var expectedLevel = expectPreRound ? GameRunLevel.PreRoundLobby : GameRunLevel.InRound;
        Assert.That(ticker.RunLevel, Is.EqualTo(expectedLevel));

        if (ticker.DummyTicker || !settings.Connected)
            return;

        var sPlayer = Server.ResolveDependency<ISharedPlayerManager>();
        var session = sPlayer.Sessions.Single();
        var status = ticker.PlayerGameStatuses[session.UserId];
        var expected = settings.InLobby
            ? PlayerGameStatus.NotReadyToPlay
            : PlayerGameStatus.JoinedGame;

        Assert.That(status, Is.EqualTo(expected));

        if (settings.InLobby)
        {
            Assert.That(session.AttachedEntity, Is.Null);
            return;
        }

        Assert.That(session.AttachedEntity, Is.Not.Null);
        Assert.That(Server.EntMan.EntityExists(session.AttachedEntity));
        Assert.That(Server.EntMan.HasComponent<MindContainerComponent>(session.AttachedEntity));
        var mindCont = Server.EntMan.GetComponent<MindContainerComponent>(session.AttachedEntity!.Value);
        Assert.That(mindCont.Mind, Is.Not.Null);
        Assert.That(Server.EntMan.TryGetComponent(mindCont.Mind, out MindComponent? mind));
        Assert.That(mind!.VisitingEntity, Is.Null);
        Assert.That(mind.OwnedEntity, Is.EqualTo(session.AttachedEntity!.Value));
        Assert.That(mind.UserId, Is.EqualTo(session.UserId));
    }
}
