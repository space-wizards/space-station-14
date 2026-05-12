using Content.Server.GameTicking;
using Content.Shared.FeedbackSystem;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.FeedbackSystem;

public sealed partial class FeedbackSystem : EntitySystem
{
    [Dependency] private readonly ServerFeedbackManager _feedbackManager = null!;
    [Dependency] private readonly GameTicker _gameTicker = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent args)
    {
        var validPopups = new List<ProtoId<FeedbackPopupPrototype>>();
        var notValidPopups = new List<ProtoId<FeedbackPopupPrototype>>();

        foreach (var feedback in _feedbackManager.GetOriginFeedbackPrototypes(true, true))
        {
            if (_gameTicker.IsGameRuleAdded(_prototypeManager.Index(feedback).RuleWhitelist))
                validPopups.Add(feedback);
            else
                notValidPopups.Add(feedback);
        }

        if (validPopups.Count > 0)
            _feedbackManager.SendToAllSessions(validPopups);

        if (notValidPopups.Count > 0)
            _feedbackManager.SendToAllSessions(notValidPopups, true);
    }
}
