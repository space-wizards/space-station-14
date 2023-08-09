using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using SpaceWizards.Sodium.Interop;

namespace Content.Server.Tips;

/// <summary>
///     Handles periodically displaying gameplay tips to all players ingame.
/// </summary>
public sealed class TipsSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private bool _tipsEnabled;
    private float _tipTimeOutOfRound;
    private float _tipTimeInRound;
    private string _tipsDataset = "";

    [ViewVariables(VVAccess.ReadWrite)]
    private TimeSpan _nextTipTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        _cfg.OnValueChanged(CCVars.TipFrequencyOutOfRound, SetOutOfRound, true);
        _cfg.OnValueChanged(CCVars.TipFrequencyInRound, SetInRound, true);
        _cfg.OnValueChanged(CCVars.TipsEnabled, SetEnabled, true);
        _cfg.OnValueChanged(CCVars.TipsDataset, SetDataset, true);

        RecalculateNextTipTime();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_tipsEnabled)
            return;

        if (_nextTipTime != TimeSpan.Zero && _timing.CurTime > _nextTipTime)
        {
            AnnounceRandomTip();
            RecalculateNextTipTime();
        }
    }

    private void SetOutOfRound(float value)
    {
        _tipTimeOutOfRound = value;
    }

    private void SetInRound(float value)
    {
        _tipTimeInRound = value;
    }

    private void SetEnabled(bool value)
    {
        _tipsEnabled = value;

        if (_nextTipTime != TimeSpan.Zero)
            RecalculateNextTipTime();
    }

    private void SetDataset(string value)
    {
        _tipsDataset = value;
    }

    private void AnnounceRandomTip()
    {
        if (!_prototype.TryIndex<DatasetPrototype>(_tipsDataset, out var tips))
            return;

        var tip = _random.Pick(tips.Values);
        var msg = Loc.GetString("tips-system-chat-message-wrap", ("tip", tip));

        _chat.ChatMessageToManyFiltered(Filter.Broadcast(), ChatChannel.OOC, tip, msg,
            EntityUid.Invalid, false, false, Color.MediumPurple);
    }

    private void RecalculateNextTipTime()
    {
        if (_ticker.RunLevel == GameRunLevel.InRound)
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeInRound);
        }
        else
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeOutOfRound);
        }
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // reset for lobby -> inround
        // reset for inround -> post but not post -> lobby
        if (ev.New == GameRunLevel.InRound || ev.Old == GameRunLevel.InRound)
        {
            RecalculateNextTipTime();
        }
    }
}
