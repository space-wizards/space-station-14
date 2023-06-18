// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Afk;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CryopodSSD;

public sealed class AfkSSDSystem : EntitySystem
{
    [Dependency] private readonly CryopodSSDSystem _cryopodSSDSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _afkTeleportTocryo;

    private readonly Dictionary<(EntityUid, NetUserId), (TimeSpan, bool)> _entityEnteredSSDTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.AfkTeleportToCryo, SetAfkTeleportToCryo, true);
        _playerManager.PlayerStatusChanged += OnPlayerChange;
    }

    private void SetAfkTeleportToCryo(float value)
        => _afkTeleportTocryo = value;

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.AfkTeleportToCryo, SetAfkTeleportToCryo);
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _entityEnteredSSDTimes.Where(uid => HasComp<MindComponent>(uid.Key.Item1)))
        {
            if (pair.Value.Item2 && IsTeleportAfkToCryoTime(pair.Value.Item1)
                && _cryopodSSDSystem.TeleportEntityToCryoStorageWithDelay(pair.Key.Item1))
            {
                _entityEnteredSSDTimes.Remove(pair.Key);
            }
        }
    }
    
    private bool IsTeleportAfkToCryoTime(TimeSpan time)
    {
        var timeOut = TimeSpan.FromSeconds(_afkTeleportTocryo);
        return _gameTiming.CurTime - time > timeOut;
    }

    private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
    {

        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                if (e.Session.AttachedEntity is null
                    || !HasComp<MindComponent>(e.Session.AttachedEntity)
                    || !HasComp<BodyComponent>(e.Session.AttachedEntity))
                {
                    break;
                }
                
                if (!_preferencesManager.TryGetCachedPreferences(e.Session.UserId, out var preferences)
                    || preferences.SelectedCharacter is not HumanoidCharacterProfile humanoidPreferences)
                {
                    break;
                }
                _entityEnteredSSDTimes[(e.Session.AttachedEntity.Value, e.Session.UserId)]
                    = (_gameTiming.CurTime, humanoidPreferences.TeleportAfkToCryoStorage);
                break;
            case SessionStatus.Connected:
                if (_entityEnteredSSDTimes
                    .TryFirstOrNull(item => item.Key.Item2 == e.Session.UserId, out var item))
                {
                    _entityEnteredSSDTimes.Remove(item.Value.Key);
                }

                break;
        }
    }
}