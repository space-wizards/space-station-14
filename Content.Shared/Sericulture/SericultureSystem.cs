using Content.Shared.Actions;
using Content.Shared.Cloning.Events;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.Nutrition.Components;
using Content.Shared.Stacks;

namespace Content.Shared.Sericulture;

/// <summary>
/// Allows mobs to produce materials with <see cref="SericultureComponent"/>.
/// </summary>
public abstract partial class SharedSericultureSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedStackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SericultureComponent, SericultureActionEvent>(OnSericultureStart);
        SubscribeLocalEvent<SericultureComponent, SericultureDoAfterEvent>(OnSericultureDoAfter);
        SubscribeLocalEvent<SericultureComponent, CloningEvent>(OnClone);
    }

    private void OnClone(Entity<SericultureComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<SericultureComponent>();
        cloneComp.PopupText = ent.Comp.PopupText;
        cloneComp.EntityProduced = ent.Comp.EntityProduced;
        cloneComp.ProductionLength = ent.Comp.ProductionLength;
        cloneComp.HungerCost = ent.Comp.HungerCost;
        cloneComp.MinHungerThreshold = ent.Comp.MinHungerThreshold;
        AddComp(args.CloneUid, cloneComp, true);
    }

    private void OnSericultureStart(EntityUid uid, SericultureComponent comp, SericultureActionEvent args)
    {
        if (!TryComp<SatiationComponent>(uid, out var satiationComponent) ||
            !_satiation.IsValueInRange((uid, satiationComponent), SatiationSystem.Hunger, above: comp.MinHungerThreshold, hypotheticalValueDelta: -comp.HungerCost))
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, comp.ProductionLength, new SericultureDoAfterEvent(), uid)
        {
            // I'm not sure if more things should be put here, but imo ideally it should probably be set in the component/YAML. Not sure if this is currently possible.
            BreakOnMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnSericultureDoAfter(EntityUid uid, SericultureComponent comp, SericultureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        // A check, just incase the doafter is somehow performed when the entity is not in the right hunger state.
        if (!TryComp<SatiationComponent>(uid, out var satiationComponent) ||
            !_satiation.IsValueInRange((uid, satiationComponent), SatiationSystem.Hunger, above: comp.MinHungerThreshold, hypotheticalValueDelta: -comp.HungerCost))
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        _satiation.ModifyValue((uid, satiationComponent), SatiationSystem.Hunger, -comp.HungerCost);

        if (!_netManager.IsClient) // Have to do this because spawning stuff in shared is CBT.
        {
            var newEntity = SpawnNextToOrDrop(comp.EntityProduced, uid);

            _stackSystem.TryMergeToHands(newEntity, uid);
        }

        args.Repeat = true;
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class SericultureActionEvent : InstantActionEvent;

/// <summary>
/// Is relayed at the end of the sericulturing doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SericultureDoAfterEvent : SimpleDoAfterEvent;
