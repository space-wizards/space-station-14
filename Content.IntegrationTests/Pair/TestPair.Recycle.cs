#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Pair;

// This partial class contains logic related to recycling & disposing test pairs.
public sealed partial class TestPair
{
    private bool _cleanedEmptyWorld;

    public override bool CanFastRecycle(PairSettings next) =>
        _cleanedEmptyWorld && base.CanFastRecycle(next);

    protected override async Task Cleanup()
    {
        _cleanedEmptyWorld = false;
        await base.Cleanup();
        await ResetModifiedPreferences();

        if (Settings is not PoolSettings { DummyTicker: true, Dirty: false } || Settings.MustNotBeReused)
            return;

        await Server.WaitPost(() =>
        {
            var ticker = Server.System<ServerGameTicker>();
            if (!ticker.DummyTicker || ticker.RunLevel != GameRunLevel.PreRoundLobby ||
                ticker.CurrentPreset != null || ticker.GetLastGameRule() != null)
                return;

            Server.EntMan.EventBus.RaiseEvent(Robust.Shared.GameObjects.EventSource.Local, new RoundRestartCleanupEvent());
            Server.EntMan.FlushEntities();
            _cleanedEmptyWorld = true;
        });
        if (!_cleanedEmptyWorld)
            return;

        // Make sure server deletions come in before we start lawnmowering the client entities.
        await RunUntilSynced();

        await Client.WaitPost(() =>
        {
            Client.EntMan.EventBus.RaiseEvent(Robust.Shared.GameObjects.EventSource.Local, new RoundRestartCleanupEvent());
            foreach (var uid in Client.EntMan.GetEntities().Where(uid => Client.EntMan.IsClientSide(uid)).ToArray())
            {
                Client.EntMan.DeleteEntity(uid);
            }
        });
    }

    protected override async Task ValidateCleanup()
    {
        await base.ValidateCleanup();
        if (!_cleanedEmptyWorld)
            return;

        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            if (Server.EntMan.EntityCount != 0)
            {
                Assert.Fail("Server entities remained after pair cleanup:\n"
                            + DescribeRemainingEntities(Server.EntMan, Server.Timing));
            }
        });
        await Client.WaitAssertion(() =>
        {
            if (Client.EntMan.EntityCount != 0)
            {
                Assert.Fail("Client entities remained after pair cleanup:\n"
                            + DescribeRemainingEntities(Client.EntMan, Client.Timing));
            }
        });
    }

    private static string DescribeRemainingEntities(IEntityManager entMan, IGameTiming timing)
    {
        const int maxEntities = 16;

        var sb = new StringBuilder();
        sb.AppendLine($"Entity count: {entMan.EntityCount}, current tick: {timing.CurTick}");

        var count = 0;
        foreach (var uid in entMan.GetEntities())
        {
            if (count++ >= maxEntities)
            {
                sb.AppendLine("...");
                break;
            }

            // The entity heisenbugs are frying me.
            var meta = entMan.GetComponent<MetaDataComponent>(uid);
            var parent = entMan.TryGetComponent(uid, out TransformComponent? xform)
                ? $"{entMan.ToPrettyString(xform.ParentUid)} (map {xform.MapID})"
                : "<no xform>";
            var components = string.Join(", ",
                entMan.GetComponents(uid).Select(c => c.GetType().Name).OrderBy(name => name));

            sb.AppendLine($"- {entMan.ToPrettyString(uid)}, prototype: {meta.EntityPrototype?.ID ?? "<none>"}, "
                          + $"created on tick {meta.CreationTick}, life stage: {meta.EntityLifeStage}, "
                          + $"paused: {meta.EntityPaused}, parent: {parent}");
            sb.AppendLine($"  components: {components}");
        }

        return sb.ToString();
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
        var settings = (PoolSettings) next;
        var gameTicker = Server.System<ServerGameTicker>();
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Resetting round.");

        if (settings.DummyTicker)
        {
            await Server.WaitPost(() =>
            {
                if (!gameTicker.DummyTicker || gameTicker.RunLevel != GameRunLevel.PreRoundLobby ||
                    gameTicker.CurrentPreset != null || gameTicker.GetLastGameRule() != null)
                {
                    // Finish the round before disabling its ticker. Keep the lobby enabled so
                    // cleanup doesn't start another map that would immediately be dumped and waste time.
                    Server.CfgMan.SetCVar(CCVars.GameDummyTicker, false);
                    Server.CfgMan.SetCVar(CCVars.GameLobbyEnabled, true);
                    gameTicker.RestartRound();
                }
                else
                {
                    // RestartRound is a NOOP for a dummy ticker.
                    Server.EntMan.FlushEntities();
                }
            });
            await ApplySettings(next);
        }
        else
        {
            await ApplySettings(next);
            await Server.WaitPost(gameTicker.RestartRound);
        }

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

        var ticker = Server.System<ServerGameTicker>();
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
