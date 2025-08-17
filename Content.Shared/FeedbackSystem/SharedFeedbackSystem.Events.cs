using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

public sealed partial class SharedFeedbackSystem
{
    [Dependency] private readonly SharedFeedbackSystem _feedback = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _confg = default!;

    private List<string> _validOrigins = [];

    private void EventInitialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);

        Subs.CVar(_confg, CCVars.FeedbackValidOrigins, OnFeedbackOriginsUpdated, true);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // Send all the popups to players at the end of the round.
        var actors = EntityQueryEnumerator<ActorComponent>();

        var feedbackProtypes = _proto.EnumeratePrototypes<FeedbackPopupPrototype>()
            .Where(x => x.ShowRoundEnd && _validOrigins.Contains(x.PopupOrigin))
            .Select(x => new ProtoId<FeedbackPopupPrototype>(x.ID))
            .OrderBy(x => x.Id)
            .ToList();

        if (feedbackProtypes.Count == 0)
            return;

        while (actors.MoveNext(out _, out var actorComp))
        {
            _feedback.SendPopupsSession(actorComp.PlayerSession, feedbackProtypes);
        }
    }

    private void OnFeedbackOriginsUpdated(string newOrigins)
    {
        _validOrigins =  newOrigins.Split(' ').ToList();
    }
}
