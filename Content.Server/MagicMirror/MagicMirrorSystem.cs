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

public sealed class MagicMirrorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<MagicMirrorComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectMessage>(OnMagicMirrorSelect);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorChangeColorMessage>(OnTryMagicMirrorChangeColor);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorAddSlotMessage>(OnTryMagicMirrorAddSlot);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorRemoveSlotMessage>(OnTryMagicMirrorRemoveSlot);

        SubscribeLocalEvent<MagicMirrorComponent, AfterInteractEvent>(OnMagicMirrorInteract);

        SubscribeLocalEvent<MagicMirrorComponent, SelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, ChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, RemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<MagicMirrorComponent, AddSlotDoAfterEvent>(OnAddSlotDoAfter);
    }

    private void OnMagicMirrorInteract(Entity<MagicMirrorComponent> mirror, ref AfterInteractEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)) return;
        if (TryComp<HumanoidAppearanceComponent>(args.Target, out var humanoid))
        {
            mirror.Comp.Target = new Entity<HumanoidAppearanceComponent>(args.Target.Value, humanoid);
            UpdateInterface(mirror.Owner, mirror.Comp.Target.Value.Owner, actor.PlayerSession);
            Log.Debug($"Target {mirror.Comp.Target}!");
        };
    }

    private void OnOpenUIAttempt(EntityUid uid, MagicMirrorComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            args.Cancel();
    }

    private void OnMagicMirrorSelect(EntityUid uid, MagicMirrorComponent component, MagicMirrorSelectMessage message)
    {
        if (component.Target == null) return;
        if (message.Session.AttachedEntity == null) return;

        var doAfter = new SelectDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Session.AttachedEntity.Value, component.SelectSlotTime, doAfter, uid, target: component.Target.Value.Owner, used: uid)
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
    private void OnSelectSlotDoAfter(EntityUid uid, MagicMirrorComponent component, SelectDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null || args.Cancelled)
            return;
        if (component.Target == null) return;

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

        _humanoid.SetMarkingId(component.Target.Value.Owner, category, args.Message.Slot, args.Message.Marking);

        UpdateInterface(uid, component.Target.Value.Owner, args.Message.Session);
    }

    private void OnTryMagicMirrorChangeColor(EntityUid uid, MagicMirrorComponent component, MagicMirrorChangeColorMessage message)
    {
        if (component.Target == null) return;
        if (message.Session.AttachedEntity == null) return;

        var doAfter = new ChangeColorDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Session.AttachedEntity.Value, component.ChangeSlotTime, doAfter, uid, target: component.Target.Value.Owner, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnUserMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        });
    }
    private void OnChangeColorDoAfter(EntityUid uid, MagicMirrorComponent component, ChangeColorDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null || args.Cancelled)
            return;
        if (component.Target == null) return;

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

        _humanoid.SetMarkingColor(component.Target.Value.Owner, category, args.Message.Slot, args.Message.Colors);

        // using this makes the UI feel like total ass
        // UpdateInterface(uid, component.Target, message.Session);
    }

    private void OnTryMagicMirrorRemoveSlot(EntityUid uid, MagicMirrorComponent component, MagicMirrorRemoveSlotMessage message)
    {
        if (component.Target == null) return;
        if (message.Session.AttachedEntity == null) return;

        var doAfter = new RemoveSlotDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Session.AttachedEntity.Value, component.RemoveSlotTime, doAfter, uid, target: component.Target.Value.Owner, used: uid)
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
    private void OnRemoveSlotDoAfter(EntityUid uid, MagicMirrorComponent component, RemoveSlotDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null || args.Cancelled)
            return;

        if (component.Target == null) return;

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

        _humanoid.RemoveMarking(component.Target.Value.Owner, category, args.Message.Slot);

        UpdateInterface(uid, component.Target.Value.Owner, args.Message.Session);
    }

    private void OnTryMagicMirrorAddSlot(EntityUid uid, MagicMirrorComponent component, MagicMirrorAddSlotMessage message)
    {
        if (component.Target == null) return;
        if (message.Session.AttachedEntity == null) return;

        var doAfter = new AddSlotDoAfterEvent(message);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Session.AttachedEntity.Value, component.AddSlotTime, doAfter, uid, target: component.Target.Value.Owner, used: uid)
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
    private void OnAddSlotDoAfter(EntityUid uid, MagicMirrorComponent component, AddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null || args.Cancelled)
            return;

        if (component.Target == null) return;

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

        var marking = _markings.MarkingsByCategoryAndSpecies(category, component.Target.Value.Comp.Species).Keys.FirstOrDefault();
        if (string.IsNullOrEmpty(marking))
        {
            return;
        }

        _humanoid.AddMarking(component.Target.Value.Owner, marking, Color.Black);

        UpdateInterface(uid, component.Target.Value.Owner, args.Message.Session);
    }

    private void UpdateInterface(EntityUid uid, EntityUid playerUid, ICommonSession session)
    {
        if (!TryComp<HumanoidAppearanceComponent>(playerUid, out var humanoid)) return;
        if (session is not { } player) return;

        var hair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : new();

        var facialHair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : new();

        var msg = new MagicMirrorUiData(
            humanoid.Species,
            hair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        _uiSystem.TrySendUiMessage(uid, MagicMirrorUiKey.Key, msg, player);
    }

    private void AfterUIOpen(EntityUid uid, MagicMirrorComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(args.User, out var humanoid)) return;
        component.Target = new Entity<HumanoidAppearanceComponent>(args.User, humanoid);

        UpdateInterface(uid, args.User, args.Session);
    }
}
