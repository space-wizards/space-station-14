using Content.Server.GameTicking;
using Content.Shared.CrewMedal;
using System.Linq;
using System.Text;

namespace Content.Server.CrewMedal;

public sealed class CrewMedalSystem : SharedCrewMedalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        // medal name, recipient name, reason
        var medals = new List<(string, string, string)>();
        var query = EntityQueryEnumerator<CrewMedalComponent>();
        while (query.MoveNext(out var uid, out var crewMedalComp))
        {
            medals.Add((Name(uid), crewMedalComp.Recipient, crewMedalComp.Reason));
        }
        var count = medals.Count;
        if (count == 0)
            return;

        medals.OrderBy(f => f.Item2);
        var result = new StringBuilder();
        result.AppendLine(Loc.GetString("comp-crew-medal-round-end-result", ("count", count)));
        foreach (var medal in medals)
        {
            result.Append("- ").AppendLine(Loc.GetString("comp-crew-medal-round-end-list", ("recipient", medal.Item2), ("medal", medal.Item1)));
            result.Append("  ").AppendLine(medal.Item3);
        }
        ev.AddLine(result.AppendLine().ToString());
    }
}
