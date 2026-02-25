using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.FeedbackSystem;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.FeedbackSystem;

public sealed class FeedbackSystem : EntitySystem
{
    [Dependency] private readonly ServerFeedbackManager _feedbackManager = null!;
    [Dependency] private readonly GameTicker _gameTicker = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev, EntitySessionEventArgs args)
    {
        var showFeedbackPrototypes = _feedbackManager.GetOriginFeedbackPrototypes(true, true)
            .Select(x => _prototypeManager.Index(x))
            .Where(x => _gameTicker.IsGameRuleActive(x.RuleId!))
            .Select(x => new ProtoId<FeedbackPopupPrototype>(x.ID))
            .OrderBy(x => x.Id)
            .ToList();

        var notShowFeedbackPrototypes = _feedbackManager.GetOriginFeedbackPrototypes(true, true)
            .Select(x => _prototypeManager.Index(x))
            .Where(x => !_gameTicker.IsGameRuleActive(x.RuleId!))
            .Select(x => new ProtoId<FeedbackPopupPrototype>(x.ID))
            .OrderBy(x => x.Id)
            .ToList();

        _feedbackManager.SendToAllSessions(showFeedbackPrototypes);
        _feedbackManager.SendToAllSessions(notShowFeedbackPrototypes, true);
    }
}
