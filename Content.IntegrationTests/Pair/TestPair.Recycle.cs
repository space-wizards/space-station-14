#nullable enable
using System.IO;
using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Client;
using Robust.Server.Player;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Pair;

// This partial class contains logic related to recycling & disposing test pairs.
public sealed partial class TestPair : IAsyncDisposable
{
    public PairState State { get; private set; } = PairState.Ready;

    private async Task OnDirtyDispose()
    {
        var usageTime = Watch.Elapsed;
        Watch.Restart();
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Test gave back pair {Id} in {usageTime.TotalMilliseconds} ms");
        Kill();
        var disposeTime = Watch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Disposed pair {Id} in {disposeTime.TotalMilliseconds} ms");
        // Test pairs should only dirty dispose if they are failing. If they are not failing, this probably happened
        // because someone forgot to clean-return the pair.
        Assert.Warn("Test was dirty-disposed.");
    }

    private async Task OnCleanDispose()
    {
        if (TestMap != null)
        {
            await Server.WaitPost(() => Server.EntMan.DeleteEntity(TestMap.MapUid));
            TestMap = null;
        }

        var usageTime = Watch.Elapsed;
        Watch.Restart();
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Test borrowed pair {Id} for {usageTime.TotalMilliseconds} ms");
        // Let any last minute failures the test cause happen.
        await ReallyBeIdle();
        if (!Settings.Destructive)
        {
            if (Client.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the client in pair {Id}:", Client.UnhandledException);
            }

            if (Server.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the server in pair {Id}:", Server.UnhandledException);
            }
        }

        if (Settings.MustNotBeReused)
        {
            Kill();
            await ReallyBeIdle();
            await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Clean disposed in {Watch.Elapsed.TotalMilliseconds} ms");
            return;
        }

        var sRuntimeLog = Server.ResolveDependency<IRuntimeLog>();
        if (sRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Server logged exceptions");
        var cRuntimeLog = Client.ResolveDependency<IRuntimeLog>();
        if (cRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Client logged exceptions");

        var returnTime = Watch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: PoolManager took {returnTime.TotalMilliseconds} ms to put pair {Id} back into the pool");
    }

    public async ValueTask CleanReturnAsync()
    {
        if (State != PairState.InUse)
            throw new Exception($"{nameof(CleanReturnAsync)}: Unexpected state. Pair: {Id}. State: {State}.");

        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Return of pair {Id} started");
        State = PairState.CleanDisposed;
        await OnCleanDispose();
        State = PairState.Ready;
        PoolManager.NoCheckReturn(this);
        ClearContext();
    }

    public async ValueTask DisposeAsync()
    {
        switch (State)
        {
            case PairState.Dead:
            case PairState.Ready:
                break;
            case PairState.InUse:
                await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Dirty return of pair {Id} started");
                await OnDirtyDispose();
                PoolManager.NoCheckReturn(this);
                ClearContext();
                break;
            default:
                throw new Exception($"{nameof(DisposeAsync)}: Unexpected state. Pair: {Id}. State: {State}.");
        }
    }

    public async Task CleanPooledPair(PoolSettings settings, TextWriter testOut)
    {
        Settings = default!;
        Watch.Restart();
        await testOut.WriteLineAsync($"Recycling...");

        var gameTicker = Server.System<GameTicker>();
        var cNetMgr = Client.ResolveDependency<IClientNetManager>();

        await RunTicksSync(1);

        // Disconnect the client if they are connected.
        if (cNetMgr.IsConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Disconnecting client.");
            await Client.WaitPost(() => cNetMgr.ClientDisconnect("Test pooling cleanup disconnect"));
            await RunTicksSync(1);
        }
        Assert.That(cNetMgr.IsConnected, Is.False);

        // Move to pre-round lobby. Required to toggle dummy ticker on and off
        if (gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Restarting server.");
            Assert.That(gameTicker.DummyTicker, Is.False);
            Server.CfgMan.SetCVar(CCVars.GameLobbyEnabled, true);
            await Server.WaitPost(() => gameTicker.RestartRound());
            await RunTicksSync(1);
        }

        //Apply Cvars
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        await PoolManager.SetupCVars(Client, settings);
        await PoolManager.SetupCVars(Server, settings);
        await RunTicksSync(1);

        // Restart server.
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Restarting server again");
        await Server.WaitPost(() => gameTicker.RestartRound());
        await RunTicksSync(1);

        // Connect client
        if (settings.ShouldBeConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Connecting client");
            Client.SetConnectTarget(Server);
            await Client.WaitPost(() => cNetMgr.ClientConnect(null!, 0, null!));
        }

        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Idling");
        await ReallyBeIdle();
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Done recycling");
    }

    public void ValidateSettings(PoolSettings settings)
    {
        var cfg = Server.CfgMan;
        Assert.That(cfg.GetCVar(CCVars.AdminLogsEnabled), Is.EqualTo(settings.AdminLogsEnabled));
        Assert.That(cfg.GetCVar(CCVars.GameLobbyEnabled), Is.EqualTo(settings.InLobby));
        Assert.That(cfg.GetCVar(CCVars.GameDummyTicker), Is.EqualTo(settings.UseDummyTicker));

        var entMan = Server.ResolveDependency<EntityManager>();
        var ticker = entMan.System<GameTicker>();
        Assert.That(ticker.DummyTicker, Is.EqualTo(settings.UseDummyTicker));

        var expectPreRound = settings.InLobby | settings.DummyTicker;
        var expectedLevel = expectPreRound ? GameRunLevel.PreRoundLobby : GameRunLevel.InRound;
        Assert.That(ticker.RunLevel, Is.EqualTo(expectedLevel));

        var baseClient = Client.ResolveDependency<IBaseClient>();
        var netMan = Client.ResolveDependency<INetManager>();
        Assert.That(netMan.IsConnected, Is.Not.EqualTo(!settings.ShouldBeConnected));

        if (!settings.ShouldBeConnected)
            return;

        Assert.That(baseClient.RunLevel, Is.EqualTo(ClientRunLevel.InGame));
        var cPlayer = Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        var sPlayer = Server.ResolveDependency<IPlayerManager>();
        Assert.That(sPlayer.Sessions.Count(), Is.EqualTo(1));
        var session = sPlayer.Sessions.Single();
        Assert.That(cPlayer.LocalPlayer?.Session.UserId, Is.EqualTo(session.UserId));

        if (ticker.DummyTicker)
            return;

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
        Assert.That(entMan.EntityExists(session.AttachedEntity));
        Assert.That(entMan.HasComponent<MindContainerComponent>(session.AttachedEntity));
        var mindCont = entMan.GetComponent<MindContainerComponent>(session.AttachedEntity!.Value);
        Assert.That(mindCont.Mind, Is.Not.Null);
        Assert.That(entMan.TryGetComponent(mindCont.Mind, out MindComponent? mind));
        Assert.That(mind!.VisitingEntity, Is.Null);
        Assert.That(mind.OwnedEntity, Is.EqualTo(session.AttachedEntity!.Value));
        Assert.That(mind.UserId, Is.EqualTo(session.UserId));
    }
}
