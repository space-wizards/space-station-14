using System.Linq;
using Content.Server.Humanoid;
using Content.Server.UserInterface;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.MagicMirror;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Players;

namespace Content.Server.MagicMirror;

public sealed class MagicMirrorSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<MagicMirrorComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectMessage>(OnMagicMirrorSelect);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorChangeColorMessage>(OnMagicMirrorChangeColor);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorAddSlotMessage>(OnMagicMirrorAddSlot);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorRemoveSlotMessage>(OnMagicMirrorRemoveSlot);
    }

    private void OnOpenUIAttempt(EntityUid uid, MagicMirrorComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidComponent>(args.User))
            args.Cancel();
    }

    private void OnMagicMirrorSelect(EntityUid uid, MagicMirrorComponent component,
        MagicMirrorSelectMessage message)
    {
        if (message.Session.AttachedEntity == null || !TryComp<HumanoidComponent>(message.Session.AttachedEntity.Value, out var humanoid))
        {
            return;
        }

        var category = MarkingCategories.Hair;
        switch (message.Category)
        {
            case MagicMirrorCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case MagicMirrorCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingId(message.Session.AttachedEntity.Value, category, message.Slot, message.Marking);

        UpdateInterface(uid, message.Session.AttachedEntity.Value, message.Session);
    }

    private void OnMagicMirrorChangeColor(EntityUid uid, MagicMirrorComponent component,
        MagicMirrorChangeColorMessage message)
    {
        if (message.Session.AttachedEntity == null || !TryComp<HumanoidComponent>(message.Session.AttachedEntity.Value, out var humanoid))
        {
            return;
        }

        var category = MarkingCategories.Hair;
        switch (message.Category)
        {
            case MagicMirrorCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case MagicMirrorCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingColor(message.Session.AttachedEntity.Value, category, message.Slot, message.Colors);

        // using this makes the UI feel like total ass
        // UpdateInterface(uid, message.Session.AttachedEntity.Value, message.Session);
    }

    private void OnMagicMirrorRemoveSlot(EntityUid uid, MagicMirrorComponent component,
        MagicMirrorRemoveSlotMessage message)
    {
        if (message.Session.AttachedEntity == null || !TryComp<HumanoidComponent>(message.Session.AttachedEntity.Value, out var humanoid))
        {
            return;
        }

        var category = MarkingCategories.Hair;
        switch (message.Category)
        {
            case MagicMirrorCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case MagicMirrorCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.RemoveMarking(message.Session.AttachedEntity.Value, category, message.Slot);

        UpdateInterface(uid, message.Session.AttachedEntity.Value, message.Session);
    }

    private void OnMagicMirrorAddSlot(EntityUid uid, MagicMirrorComponent component,
        MagicMirrorAddSlotMessage message)
    {
        if (message.Session.AttachedEntity == null || !TryComp<HumanoidComponent>(message.Session.AttachedEntity.Value, out var humanoid))
        {
            return;
        }

        var category = MarkingCategories.Hair;
        switch (message.Category)
        {
            case MagicMirrorCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case MagicMirrorCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        var marking = _markings.MarkingsByCategoryAndSpecies(category, humanoid.Species).Keys.FirstOrDefault();
        if (string.IsNullOrEmpty(marking))
        {
            return;
        }

        _humanoid.AddMarking(message.Session.AttachedEntity.Value, marking, Color.Black);

        UpdateInterface(uid, message.Session.AttachedEntity.Value, message.Session);
    }

    private void UpdateInterface(EntityUid uid, EntityUid playerUid, ICommonSession session, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(playerUid, ref humanoid) || session is not IPlayerSession player)
        {
            return;
        }

        var hair = humanoid.CurrentMarkings.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : new();

        var facialHair = humanoid.CurrentMarkings.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : new();

        var msg = new MagicMirrorUiData(
            humanoid.Species,
            hair,
            humanoid.CurrentMarkings.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.CurrentMarkings.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        _uiSystem.TrySendUiMessage(uid, MagicMirrorUiKey.Key, msg, player);
    }

    private void AfterUIOpen(EntityUid uid, MagicMirrorComponent component, AfterActivatableUIOpenEvent args)
    {
        var looks = Comp<HumanoidComponent>(args.User);
        var actor = Comp<ActorComponent>(args.User);

        UpdateInterface(uid, args.User, args.Session);
    }
}
