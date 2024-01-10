using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// Handles borg conversion doafter starting and provides public api <see cref="TryConvert"/>.
/// The doafter handling is done serverside.
/// </summary>
public abstract class SharedBorgConverterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgConverterComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
    }

    private void OnBeforeInteractHand(Entity<BorgConverterComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !HasComp<BorgChassisComponent>(args.Target))
            return;

        Popup.PopupEntity(Loc.GetString(ent.Comp.ConvertingPopup), args.Target, args.Target);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, new BorgConversionDoAfterEvent(), target: args.Target, used: ent, eventTarget: ent)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        });
        args.Handled = true;
    }

    /// <summary>
    /// Converts a borg to the specified entity prototype.
    /// Its brain battery and name get carried across but modules are not,
    /// so the prototype should have a modules fill.
    /// </summary>
    public EntityUid? TryConvert(Entity<BorgChassisComponent?> borg, string proto)
    {
        if (!Resolve(borg, ref borg.Comp))
            return null;

        // remove brain and battery before deleting old chassis
        var brain = borg.Comp.BrainEntity;
        if (brain != null)
        {
            _container.RemoveEntity(borg, brain.Value);
        }

        EntityUid? battery = null;
        if (TryComp<PowerCellSlotComponent>(borg, out var cellSlot))
            _itemSlots.TryEject(borg, cellSlot.CellSlotId, null, out battery);

        // delete old chassis then spawn new one in its place
        var pos = Transform(borg).Coordinates;
        var name = Name(borg);
        Del(borg);

        var uid = Spawn(proto, pos);
        borg = (uid, Comp<BorgChassisComponent>(uid));
        // give name back
        _metaData.SetEntityName(borg, name);

        var xform = Transform(borg);

        // put everything back in, if anything fails they go on the floor (do not use filled prototypes)
        if (brain != null)
            _container.Insert(brain.Value, borg.Comp!.BrainContainer, xform);

        if (battery != null && TryComp<PowerCellSlotComponent>(borg, out var newCellSlot)
            && _itemSlots.TryGetSlot(borg, newCellSlot.CellSlotId, out var newSlot)
            && newSlot.ContainerSlot != null)
        {
            // not using itemslots TryInsert since need to bypass lock+panel checks
            _container.Insert(battery.Value, newSlot.ContainerSlot, force: true);
        }

        return borg;
    }
}
