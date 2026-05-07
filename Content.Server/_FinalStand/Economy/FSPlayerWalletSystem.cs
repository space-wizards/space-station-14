using System.IO;
using System.Text.Json;
using Content.Shared._FinalStand.Economy;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._FinalStand.Economy;

public sealed class FSPlayerWalletSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    private Dictionary<Guid, int> _prestige = new();
    private static readonly ResPath PrestigeSavePath = new ResPath("/fsprestige.json");

    public override void Initialize()
    {
        base.Initialize();
        LoadPrestige();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<WalletRequestEvent>(OnWalletRequested);
    }

    private void LoadPrestige()
    {
        try
        {
            if (!_res.UserData.Exists(PrestigeSavePath))
                return;

            using var stream = _res.UserData.Open(PrestigeSavePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            _prestige = JsonSerializer.Deserialize<Dictionary<Guid, int>>(reader.ReadToEnd()) ?? new();
            Log.Debug($"[FSWallet] Loaded prestige for {_prestige.Count} player(s).");
        }
        catch (Exception e)
        {
            Log.Error($"[FSWallet] Failed to load prestige data: {e.Message}");
            _prestige = new();
        }
    }

    private void SavePrestige()
    {
        try
        {
            using var stream = _res.UserData.Open(PrestigeSavePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(JsonSerializer.Serialize(_prestige));
        }
        catch (Exception e)
        {
            Log.Error($"[FSWallet] Failed to save prestige data: {e.Message}");
        }
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_mind.TryGetMind(ev.Entity, out var mindId, out var mind) || mind.UserId == null)
            return;

        var wallet = EnsureComp<FSPlayerWalletComponent>(mindId);
        if (_prestige.TryGetValue(mind.UserId.Value.UserId, out var pp))
            wallet.PerkPoints = pp;
        NotifyClient(mindId, wallet);
        Log.Debug($"[FSWallet] Attached {mind.UserId} — credits={wallet.Credits} perk={wallet.PerkPoints}");
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        if (!_mind.TryGetMind(ev.Entity, out _, out var mind) || mind.UserId == null)
            return;

        var query = EntityQueryEnumerator<FSPlayerWalletComponent, MindComponent>();
        while (query.MoveNext(out _, out var wallet, out var m))
        {
            if (m.UserId != mind.UserId)
                continue;
            _prestige[mind.UserId.Value.UserId] = wallet.PerkPoints;
            SavePrestige();
            break;
        }
    }

    /// <summary>Gives every player with a wallet credits.</summary>
    public void DistributeCredits(int amount)
    {
        var count = 0;
        var query = EntityQueryEnumerator<FSPlayerWalletComponent>();
        while (query.MoveNext(out var mindId, out var wallet))
        {
            wallet.Credits += amount;
            NotifyClient(mindId, wallet);
            count++;
        }
        Log.Debug($"[FSWallet] DistributeCredits +{amount} → {count} player(s)");
    }

    /// <summary>Gives every player perk points and saves immediately.</summary>
    public void DistributePerkPoints(int amount)
    {
        var count = 0;
        var query = EntityQueryEnumerator<FSPlayerWalletComponent, MindComponent>();
        while (query.MoveNext(out var mindId, out var wallet, out var mind))
        {
            wallet.PerkPoints += amount;
            NotifyClient(mindId, wallet);
            if (mind.UserId != null)
                _prestige[mind.UserId.Value.UserId] = wallet.PerkPoints;
            count++;
        }
        if (count > 0)
            SavePrestige();
        Log.Info($"[FSWallet] DistributePerkPoints +{amount} → {count} player(s)");
    }

    public bool TryDeductCredits(EntityUid mindId, int amount)
    {
        if (!TryComp<FSPlayerWalletComponent>(mindId, out var wallet) || wallet.Credits < amount)
            return false;
        wallet.Credits -= amount;
        NotifyClient(mindId, wallet);
        return true;
    }

    /// <summary>Flush all prestige to disk. Call at round end.</summary>
    public void SaveAll()
    {
        var query = EntityQueryEnumerator<FSPlayerWalletComponent, MindComponent>();
        while (query.MoveNext(out _, out var wallet, out var mind))
        {
            if (mind.UserId != null)
                _prestige[mind.UserId.Value.UserId] = wallet.PerkPoints;
        }
        SavePrestige();
        Log.Info($"[FSWallet] SaveAll flushed PP for {_prestige.Count} player(s)");
    }

    public void DumpWallets(IConsoleShell shell)
    {
        var found = false;
        var query = EntityQueryEnumerator<FSPlayerWalletComponent, MindComponent>();
        while (query.MoveNext(out _, out var wallet, out var mind))
        {
            shell.WriteLine($"  {mind.UserId?.ToString() ?? "unknown"} — credits={wallet.Credits}  perk={wallet.PerkPoints}");
            found = true;
        }
        if (!found)
            shell.WriteLine("  (no wallets found)");
    }

    private void OnWalletRequested(WalletRequestEvent req, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var pp = _prestige.TryGetValue(session.UserId.UserId, out var stored) ? stored : 0;
        var credits = 0;
        var query = EntityQueryEnumerator<FSPlayerWalletComponent, MindComponent>();
        while (query.MoveNext(out _, out var wallet, out var mind))
        {
            if (mind.UserId?.UserId == session.UserId.UserId)
            {
                credits = wallet.Credits;
                break;
            }
        }
        RaiseNetworkEvent(new WalletUpdatedEvent(credits, pp), Filter.SinglePlayer(session));
    }

    private void NotifyClient(EntityUid mindId, FSPlayerWalletComponent wallet)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;
        if (!_playerManager.TryGetSessionById(mind.UserId.Value, out var session))
            return;
        RaiseNetworkEvent(new WalletUpdatedEvent(wallet.Credits, wallet.PerkPoints),
            Filter.SinglePlayer(session));
    }
}
