using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Starlight.Antags.Abductor;
using Robust.Shared.Localization;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private void InitializeRoundEnd()
    {
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var uniqueVictims = new HashSet<NetEntity>();

        var query = EntityQueryEnumerator<AbductConditionComponent>();
        while (query.MoveNext(out var _, out var cond))
        {
            foreach (var victim in cond.AbductedHashs)
                uniqueVictims.Add(victim);
        }

    int total = uniqueVictims.Count;

    var abductors = new List<string>();

    var abductorsSeen = new HashSet<NetUserId>();

        var mindQuery = EntityQueryEnumerator<MindComponent>();
        while (mindQuery.MoveNext(out var _, out var mind))
        {
            if (mind.Objectives.Count == 0)
                continue;

            var isAbductor = false;
            foreach (var objective in mind.Objectives)
            {
                if (HasComp<AbductConditionComponent>(objective))
                {
                    isAbductor = true;
                    break;
                }
            }

            if (!isAbductor)
                continue;

            var userId = mind.UserId ?? mind.OriginalOwnerUserId;
            if (userId != null && _playerManager.TryGetPlayerData(userId.Value, out var pdata))
            {
                var name = pdata.ContentData()?.Name;

                if (!string.IsNullOrWhiteSpace(name) && abductorsSeen.Add(userId.Value))
                    abductors.Add(name!);
            }
        }

        if (total > 0 || abductors.Count > 0)
        {
            ev.AddLine(Loc.GetString("round-end-prepend-abductor-abducted", ("number", total)));

            if (abductors.Count > 0)
            {
                ev.AddLine(Loc.GetString("round-end-prepend-abductor-were"));
                foreach (var name in abductors)
                {
                    ev.AddLine(Loc.GetString("round-end-prepend-abductor-name", ("name", name)));
                }
            }
        }
    }
}
