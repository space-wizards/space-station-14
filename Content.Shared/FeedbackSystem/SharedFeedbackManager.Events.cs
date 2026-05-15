using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Shared.FeedbackSystem;

public abstract partial class SharedFeedbackManager : IEntityEventSubscriber
{
    [Dependency] private readonly IConfigurationManager _configManager = null!;

    private void InitSubscriptions()
    {
       _configManager.OnValueChanged(CCVars.FeedbackValidOrigins, OnFeedbackOriginsUpdated, true);
    }

    private void OnFeedbackOriginsUpdated(string newOrigins)
    {
        _validOrigins = newOrigins.Split(' ').ToList();
    }
}
