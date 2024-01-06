using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.MagicMirror;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.MagicMirror;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
public sealed class MagicMirrorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<MagicMirrorComponent, BoundUIClosedEvent>(OnUIClosed);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectMessage>(OnMagicMirrorSelect);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorChangeColorMessage>(OnTryMagicMirrorChangeColor);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorAddSlotMessage>(OnTryMagicMirrorAddSlot);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorRemoveSlotMessage>(OnTryMagicMirrorRemoveSlot);

        SubscribeLocalEvent<MagicMirrorComponent, AfterInteractEvent>(OnMagicMirrorInteract);

        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorAddSlotDoAfterEvent>(OnAddSlotDoAfter);
    }

    private void OnMagicMirrorInteract(Entity<MagicMirrorComponent> mirror, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_uiSystem.TryOpen(mirror.Owner, MagicMirrorUiKey.Key, actor.PlayerSession))
            return;

        UpdateInterface(mirror.Owner, args.Target.Value, actor.PlayerSession, mirror.Comp);
    }

    private void OnOpenUIAttempt(EntityUid uid, MagicMirrorComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            args.Cancel();
    }

    private void OnMagicMirrorSelect(EntityUid uid, MagicMirrorComponent component, MagicMirrorSelectMessage message)
    {
        if (component.Target is not { } target || message.Session.AttachedEntity is not { } user)
            return;

        var doAfter = new MagicMirrorSelectDoAfterEvent()
        {
            Category = message.Category,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.SelectSlotTime, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnUserMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        });

        _audio.PlayPvs(component.ChangeHairSound, uid);
    }

    private void OnSelectSlotDoAfter(EntityUid uid, MagicMirrorComponent component, MagicMirrorSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        var category = MarkingCategories.Hair;

        switch (args.Category)
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

        _humanoid.SetMarkingId(component.Target.Value, category, args.Message.Slot, args.Message.Marking);

        UpdateInterface(uid, component.Target.Value, args.Message.Session);
    }

    private void OnTryMagicMirrorChangeColor(EntityUid uid, MagicMirrorComponent component, MagicMirrorChangeColorMessage message)
    {
        if (component.Target is not { } target || message.Session.AttachedEntity is not { } user)
            return;

        var doAfter = new MagicMirrorChangeColorDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.ChangeSlotTime, doAfter, uid, target: target, used: uid)

        {
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnUserMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        });
    }
    private void OnChangeColorDoAfter(EntityUid uid, MagicMirrorComponent component, MagicMirrorChangeColorDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        var category = MarkingCategories.Hair;
        switch (args.Message.Category)
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

        _humanoid.SetMarkingColor(component.Target.Value, category, args.Message.Slot, args.Message.Colors);

        // using this makes the UI feel like total ass
        // UpdateInterface(uid, component.Target, message.Session);
    }

    private void OnTryMagicMirrorRemoveSlot(EntityUid uid, MagicMirrorComponent component, MagicMirrorRemoveSlotMessage message)
    {
        if (component.Target is not { } target || message.Session.AttachedEntity is not { } user)
            return;

        var doAfter = new MagicMirrorRemoveSlotDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.RemoveSlotTime, doAfter, uid, target: target, used: uid)

        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnUserMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        });

        _audio.PlayPvs(component.ChangeHairSound, uid);
    }
    private void OnRemoveSlotDoAfter(EntityUid uid, MagicMirrorComponent component, MagicMirrorRemoveSlotDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null || args.Cancelled)
            return;

        if (component.Target == null) return;

        var category = MarkingCategories.Hair;

        switch (args.Category)
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

        _humanoid.RemoveMarking(component.Target.Value, category, args.Message.Slot);

        UpdateInterface(uid, component.Target.Value, args.Message.Session);

    }

    private void OnTryMagicMirrorAddSlot(EntityUid uid, MagicMirrorComponent component, MagicMirrorAddSlotMessage message)
    {
        if (component.Target == null) return;
        if (message.Session.AttachedEntity == null) return;

        var doAfter = new MagicMirrorAddSlotDoAfterEvent();

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Session.AttachedEntity.Value, component.AddSlotTime, doAfter, uid, target: component.Target.Value, used: uid)

        {
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnUserMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        });

        _audio.PlayPvs(component.ChangeHairSound, uid);
    }
    private void OnAddSlotDoAfter(EntityUid uid, MagicMirrorComponent component, MagicMirrorAddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        var category = MarkingCategories.Hair;

        switch (args.Category)
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

        var target = component.Target;
        var humanoid = Comp<HumanoidAppearanceComponent>(target.Value);
        var marking = _markings.MarkingsByCategoryAndSpecies(category, humanoid.Species).Keys.FirstOrDefault();

        if (string.IsNullOrEmpty(marking))
        {
            return;
        }

        _humanoid.AddMarking(component.Target.Value, marking, Color.Black);

        UpdateInterface(uid, component.Target.Value, args.Message.Session);

    }

    private void UpdateInterface(EntityUid mirrorUid, EntityUid targetUid, ICommonSession observerSession, MagicMirrorComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(targetUid, out var humanoid))
            return;

        var hair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : new();

        var facialHair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : new();

        var state = new MagicMirrorUiState(
            humanoid.Species,
            hair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        component.Target = targetUid;
        _uiSystem.TrySetUiState(mirrorUid, MagicMirrorUiKey.Key, state, observerSession);
    }

    private void OnUIClosed(Entity<MagicMirrorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }
}
