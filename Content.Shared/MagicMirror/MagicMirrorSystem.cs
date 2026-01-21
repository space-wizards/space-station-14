using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.MagicMirror;

public sealed class MagicMirrorSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    private static readonly ProtoId<TagPrototype> HidesHairTag = "HidesHair";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicMirrorComponent, AfterInteractEvent>(OnMagicMirrorInteract);
        SubscribeLocalEvent<MagicMirrorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);

        Subs.BuiEvents<MagicMirrorComponent>(MagicMirrorUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIClosedEvent>(OnUiClosed);
                subs.Event<MagicMirrorSelectMessage>(OnMagicMirrorSelect);
            });

        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectDoAfterEvent>(OnSelectSlotDoAfter);

        SubscribeLocalEvent<MagicMirrorComponent, BoundUserInterfaceCheckRangeEvent>(OnMirrorRangeCheck);
    }


    private void OnMagicMirrorSelect(Entity<MagicMirrorComponent> ent, ref MagicMirrorSelectMessage args)
    {
        if (ent.Comp.Target is not { } target)
            return;

        // Check if the target getting their hair altered has any clothes that hides their hair
        if (CheckHeadSlotOrClothes(target))
        {
            _popup.PopupEntity(
                ent.Comp.Target == args.Actor
                    ? Loc.GetString("magic-mirror-blocked-by-hat-self")
                    : Loc.GetString("magic-mirror-blocked-by-hat-self-target", ("target", Identity.Entity(args.Actor, EntityManager))),
                args.Actor,
                args.Actor,
                PopupType.Medium);
            return;
        }

        if (ent.Comp.DoAfter.HasValue)
        {
            _doAfter.Cancel(target, ent.Comp.DoAfter.Value);
            ent.Comp.DoAfter = null;
        }

        var doafterTime = ent.Comp.ModifyTime;
        if (ent.Comp.Target == args.Actor)
            doafterTime /= 3;

        var doAfter = new MagicMirrorSelectDoAfterEvent()
        {
            Markings = args.Markings,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Actor, doafterTime, doAfter, ent, target: target, used: ent)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        },
            out var doAfterId);

        if (target == args.Actor)
        {
            _popup.PopupEntity(Loc.GetString("magic-mirror-change-slot-self"), target, target, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("magic-mirror-change-slot-target", ("user", Identity.Entity(args.Actor, EntityManager))), target, target, PopupType.Medium);
        }

        ent.Comp.DoAfter = doAfterId?.Index;
        _audio.PlayPredicted(ent.Comp.ChangeHairSound, ent, args.Actor);
    }

    private void OnSelectSlotDoAfter(Entity<MagicMirrorComponent> ent, ref MagicMirrorSelectDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (ent.Comp.Target != args.Target)
            return;

        foreach (var (organ, markings) in args.Markings)
        {
            if (!ent.Comp.Organs.Contains(organ))
            {
                args.Markings.Remove(organ);
                continue;
            }

            foreach (var layer in markings.Keys)
            {
                if (!ent.Comp.Layers.Contains(layer))
                    markings.Remove(layer);
            }
        }

        _visualBody.ApplyMarkings(args.Target.Value, args.Markings);
    }

    private void OnUiClosed(Entity<MagicMirrorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
        Dirty(ent);
    }

    private void OnMagicMirrorInteract(Entity<MagicMirrorComponent> mirror, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!HasComp<VisualBodyComponent>(args.Target.Value))
            return;

        UpdateInterface(mirror, args.Target.Value);
        _userInterface.TryOpenUi(mirror.Owner, MagicMirrorUiKey.Key, args.User);
    }

    private void OnMirrorRangeCheck(EntityUid uid, MagicMirrorComponent component, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        if (component.Target == null || !Exists(component.Target))
        {
            component.Target = null;
            args.Result = BoundUserInterfaceRangeResult.Fail;
            return;
        }

        if (!_interaction.InRangeUnobstructed(component.Target.Value, uid))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnAttemptOpenUI(EntityUid uid, MagicMirrorComponent component, ref ActivatableUIOpenAttemptEvent args)
    {
        var user = component.Target ?? args.User;

        if (!HasComp<VisualBodyComponent>(user))
            args.Cancel();
    }

    private void OnBeforeUIOpen(Entity<MagicMirrorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateInterface(ent, args.User);
    }

    private void UpdateInterface(Entity<MagicMirrorComponent> ent, EntityUid target)
    {
        if (!_visualBody.TryGatherMarkingsData(target, ent.Comp.Layers, out var profiles, out var markings, out var applied))
            return;

        ent.Comp.Target = target;

        foreach (var profile in profiles)
        {
            if (!ent.Comp.Organs.Contains(profile.Key))
                profiles.Remove(profile.Key);
        }

        foreach (var marking in markings)
        {
            if (!ent.Comp.Organs.Contains(marking.Key))
            {
                profiles.Remove(marking.Key);
                continue;
            }

            marking.Value.Layers.IntersectWith(ent.Comp.Layers);
        }

        foreach (var appliedPair in applied)
        {
            if (!ent.Comp.Organs.Contains(appliedPair.Key))
                applied.Remove(appliedPair.Key);
        }

        // TODO: Component states
        var state = new MagicMirrorUiState(profiles, markings, applied);
        _userInterface.SetUiState(ent.Owner, MagicMirrorUiKey.Key, state);

        Dirty(ent);
    }


    /// <summary>
    /// Helper function that checks if the wearer has anything on their head
    /// Or if they have any clothes that hides their hair
    /// </summary>
    private bool CheckHeadSlotOrClothes(EntityUid target)
    {
        if (!TryComp<InventoryComponent>(target, out var inventoryComp))
            return false;

        // any hat whatsoever will block haircutting
        if (_inventory.TryGetSlotEntity(target, "head", out _, inventoryComp))
        {
            return true;
        }

        // maybe there's some kind of armor that has the HidesHair tag as well, so check every slot for it
        var slots = _inventory.GetSlotEnumerator((target, inventoryComp), SlotFlags.WITHOUT_POCKET);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity != null && _tag.HasTag(slot.ContainedEntity.Value, HidesHairTag))
            {
                return true;
            }
        }

        return false;
    }
}

[Serializable, NetSerializable]
public enum MagicMirrorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MagicMirrorSelectMessage : BoundUserInterfaceMessage
{
    public MagicMirrorSelectMessage(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        Markings = markings;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; }
}


[Serializable, NetSerializable]
public sealed class MagicMirrorUiState : BoundUserInterfaceState
{
    public MagicMirrorUiState(Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> markings,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied)
    {
        OrganProfileData = profiles;
        OrganMarkingData = markings;
        AppliedMarkings = applied;
    }

    public NetEntity Target;

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfileData;
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> OrganMarkingData;
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> AppliedMarkings;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorSelectDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings;

    public override DoAfterEvent Clone() => this;
}
