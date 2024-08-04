using Content.Shared.Clothing;
using Content.Shared.Examine;
using Content.Shared.Popups;

namespace Content.Shared.CrewMedal;

public abstract class SharedCrewMedalSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMedalComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CrewMedalComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<CrewMedalComponent, CrewMedalReasonChangedMessage>(OnReasonChanged);
    }

    private void OnExamined(Entity<CrewMedalComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Awarded)
            return;

        var str = Loc.GetString("comp-crew-medal-inspection-text", ("recipient", ent.Comp.Recipient), ("reason", ent.Comp.Reason));
        args.PushMarkup(str);
    }

    private void OnEquipped(EntityUid uid, CrewMedalComponent medalComp, ref ClothingGotEquippedEvent args)
    {
        if (medalComp.Awarded)
            return;
        medalComp.Recipient = Name(args.Wearer);
        medalComp.Awarded = true;
        Dirty(uid, medalComp);
        _popup.PopupEntity(Loc.GetString("comp-crew-medal-award-text", ("recipient", medalComp.Recipient), ("medal", Name(uid))), uid);
    }

    private void OnReasonChanged(EntityUid uid, CrewMedalComponent medalComp, CrewMedalReasonChangedMessage args)
    {
        var text = args.Reason.Trim();
        medalComp.Reason = text[..Math.Min(medalComp.MaxChars, text.Length)];
        Dirty(uid, medalComp);

        // Log label change
        //_adminLogger.Add(LogType.Action, LogImpact.Low,
        //    $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");
    }

}
