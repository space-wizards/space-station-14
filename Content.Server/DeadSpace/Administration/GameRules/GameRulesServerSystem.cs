// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.DeadSpace.Administration.GameRules;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Administration.GameRules;

public sealed class GameRulesServerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<(TimeSpan, string), EntityUid> _ruleEntities = new();
    private readonly Dictionary<EntityUid, string> _addedByAdmin = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ClearRoundData());
        SubscribeNetworkEvent<RequestGameRulesListMessage>(OnRequestGameRulesList);
        SubscribeNetworkEvent<AddGameRuleRequestMessage>(OnAddGameRuleRequest);
    }

    private void OnAddGameRuleRequest(AddGameRuleRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_prototypeManager.HasIndex<EntityPrototype>(msg.RuleId))
            return;

        var entity = _ticker.AddGameRule(msg.RuleId);
        if (!entity.IsValid())
            return;

        if (!string.IsNullOrEmpty(msg.AdminName))
            _addedByAdmin[entity] = msg.AdminName;
    }

    private void OnRequestGameRulesList(RequestGameRulesListMessage msg, EntitySessionEventArgs args)
    {
        var allRules = _ticker.AllPreviousGameRules;
        var entries = new List<RuleEntry>();

        if (allRules.Count > 0)
        {
            var sorted = allRules.OrderBy(rule => rule.Item1).ToList();
            foreach (var (time, rule) in sorted)
            {
                var cleanRule = rule.EndsWith(" (Pending)") ? rule[..^9].Trim() : rule.Trim();
                string? admin = null;
                if (_ruleEntities.TryGetValue((time, rule), out var entity))
                    _addedByAdmin.TryGetValue(entity, out admin);

                entries.Add(new RuleEntry(time, rule, admin));
            }
        }

        var roundActive = _ticker.RunLevel == GameRunLevel.InRound;
        var roundDuration = roundActive ? _ticker.RoundDuration() : TimeSpan.Zero;

        var response = new GameRulesListResponseMessage(entries, roundDuration, roundActive);
        RaiseNetworkEvent(response, args.SenderSession);
    }

    public void RegisterRuleEntity(TimeSpan time, string ruleName, EntityUid entity)
    {
        _ruleEntities[(time, ruleName)] = entity;
    }

    public void RecordAdmin(NetEntity entity, string? adminName)
    {
        if (adminName == null)
            return;

        if (!TryGetEntity(entity, out var uid))
            return;

        _addedByAdmin[uid.Value] = adminName;
    }

    private void ClearRoundData()
    {
        _ruleEntities.Clear();
        _addedByAdmin.Clear();
    }
}
