using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackManager : IEntityEventSubscriber
{
    [Dependency] private readonly IConfigurationManager _configManager = null!;
    [Dependency] private readonly IEntityManager _entityManager = null!;

    private void InitSubscriptions()
    {
        // TODO: Move this to FeedbackUIController
        _entityManager.EventBus.SubscribeEvent<RoundEndMessageEvent>(EventSource.Local, this, OnRoundEnd, GetType());
        _configManager.OnValueChanged(CCVars.FeedbackValidOrigins, OnFeedbackOriginsUpdated, true);
    }

    private void DisposeSubscriptions()
    {
        _entityManager.EventBus.UnsubscribeEvent<RoundEndMessageEvent>(EventSource.Local, this);
        _configManager.UnsubValueChanged(CCVars.FeedbackValidOrigins, OnFeedbackOriginsUpdated);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        var feedbackProtypes = GetOriginFeedbackPrototypes(true);

        if (feedbackProtypes.Count == 0)
            return;

        SendToAllSessions(feedbackProtypes);
    }

    private void OnFeedbackOriginsUpdated(string newOrigins)
    {
        _validOrigins = newOrigins.Split(' ').ToList();
    }
}
