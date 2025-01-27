using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing;
using Content.Shared._Impstation.CrewMedal;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using System.Linq;
using System.Text;

namespace Content.Server._Impstation.CrewMedal;

public sealed class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewMedalComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CrewMedalComponent, CrewMedalReasonChangedMessage>(OnReasonChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnEquipped(Entity<CrewMedalComponent> medal, ref ClothingGotEquippedEvent args)
    {
        if (medal.Comp.Awarded)
            return;
        medal.Comp.Recipient = Identity.Name(args.Wearer, EntityManager);
        medal.Comp.Awarded = true;
        Dirty(medal);
        _popup.PopupEntity(Loc.GetString("comp-crew-medal-award-text", ("recipient", medal.Comp.Recipient), ("medal", Name(medal.Owner))), medal.Owner);
        // Log medal awarding
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Wearer):player} was awarded the {ToPrettyString(medal.Owner):entity} with the award reason \"{medal.Comp.Reason}\"");
    }

    private void OnReasonChanged(EntityUid uid, CrewMedalComponent medalComp, CrewMedalReasonChangedMessage args)
    {
        if (medalComp.Awarded)
            return;
        medalComp.Reason = args.Reason[..Math.Min(medalComp.MaxCharacters, args.Reason.Length)];
        Dirty(uid, medalComp);

        // Log medal reason change
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):entity} to apply the award reason \"{medalComp.Reason}\"");
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        // medal name, recipient name, reason
        var medals = new List<(string, string, string)>();
        var query = EntityQueryEnumerator<CrewMedalComponent>();
        while (query.MoveNext(out var uid, out var crewMedalComp))
        {
            if (crewMedalComp.Awarded)
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
            result.AppendLine(Loc.GetString("comp-crew-medal-round-end-list", ("medal", medal.Item1), ("recipient", medal.Item2), ("reason", medal.Item3)));
        }
        ev.AddLine(result.AppendLine().ToString());
    }
}
