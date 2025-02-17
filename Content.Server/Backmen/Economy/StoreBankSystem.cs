// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Store.Conditions;
using Content.Server.VendingMachines;
using Content.Shared.Backmen.Store;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Backmen.Economy;

public sealed class StoreBankSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("bankvending");
        SubscribeLocalEvent<BuyStoreBankComponent, AfterInteractEvent>(OnAfterInteract,
            before: new[] { typeof(VendingMachineSystem) });
        SubscribeLocalEvent<BuyStoreBankComponent, RestockDoAfterEvent>(OnDoAfter,
            before: new[] { typeof(VendingMachineSystem) });

        SubscribeLocalEvent<BuyStoreBankComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<BuyStoreBankComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.EmagCategories.Count == 0 || !TryComp<StoreComponent>(ent, out var store))
            return;
        foreach (var emagCategory in ent.Comp.EmagCategories)
        {
            store.Categories.Add(emagCategory);
        }
        Dirty(ent, store);
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.SparkSound, ent);
    }

    private void TryRestockInventory(EntityUid uid, IEnumerable<string>? category = null,
        BuyStoreBankComponent? vendComponent = null, StoreComponent? storeComponent = null)
    {
        if (!Resolve(uid, ref vendComponent) || !Resolve(uid, ref storeComponent))
            return;

        //var _category = category?.ToArray() ?? Array.Empty<string>();
        foreach (var storeComponentListing in storeComponent.FullListingsCatalog.Where(x =>
                     storeComponent.Categories.Any(z => x.Categories.Contains(z))))
        {
            var limit = storeComponentListing?.Conditions?.OfType<ListingLimitedStockCondition>().FirstOrDefault();
            if ((limit == null && category != null) || storeComponentListing == null)
                continue;
            if (limit == null)
            {
                storeComponentListing.PurchaseAmount = 0;
            }
            else
            {
                storeComponentListing.PurchaseAmount -= limit.Stock;
            }
        }

        Dirty(uid, storeComponent);
        //UpdateVendingMachineInterfaceState(uid, vendComponent);
        //TryUpdateVisualState(uid, vendComponent);
    }

    private void OnDoAfter(EntityUid uid, BuyStoreBankComponent component, RestockDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used == null)
            return;

        if (!TryComp<VendingMachineRestockComponent>(args.Args.Used, out var restockComponent))
        {
            _sawmill.Error(
                $"{ToPrettyString(args.Args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.Args.Used.Value)} which did not have a VendingMachineRestockComponent.");
            return;
        }

        TryRestockInventory(uid, restockComponent.CanRestock, component);

        _popup.PopupEntity(
            Loc.GetString("vending-machine-restock-done", ("this", args.Args.Used), ("user", args.Args.User),
                ("target", uid)), args.Args.User, PopupType.Medium);

        _audio.PlayPvs(restockComponent.SoundRestockDone, uid, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

        Del(args.Args.Used.Value);

        args.Handled = true;
    }

    public bool TryAccessMachine(EntityUid uid,
        BuyStoreBankComponent restock,
        StoreComponent machineComponent,
        EntityUid user,
        EntityUid target)
    {
        if (!TryComp<WiresPanelComponent>(target, out var panel) || !panel.Open)
        {
            _popup.PopupCursor(Loc.GetString("vending-machine-restock-needs-panel-open",
                    ("this", uid),
                    ("user", user),
                    ("target", target)),
                user);

            return false;
        }

        return true;
    }

    private bool TryMatchPackageToMachine(EntityUid uid,
        BuyStoreBankComponent component,
        StoreComponent machineComponent,
        EntityUid user,
        EntityUid target)
    {
        if (!component.CanRestock.Any(x => machineComponent.Categories.Contains(x)))
        {
            _popup.PopupCursor(Loc.GetString("vending-machine-restock-invalid-inventory", ("this", uid), ("user", user),
                ("target", target)), user);

            return false;
        }

        return true;
    }

    private void OnAfterInteract(EntityUid uid, BuyStoreBankComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach || args.Handled)
            return;

        if (!TryComp<StoreComponent>(args.Target, out var machineComponent))
            return;

        if (!TryMatchPackageToMachine(uid, component, machineComponent, args.User, target))
            return;

        if (!TryAccessMachine(uid, component, machineComponent, args.User, target))
            return;

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, (float)component.RestockDelay.TotalSeconds,
            new RestockDoAfterEvent(), target,
            target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _popup.PopupEntity(Loc.GetString("vending-machine-restock-start", ("this", uid), ("user", args.User),
                ("target", target)),
            args.User,
            PopupType.Medium);

        _audio.PlayPvs(component.SoundRestockStart, uid, AudioParams.Default.WithVolume(-8f));
    }
}
