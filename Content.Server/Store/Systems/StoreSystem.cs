using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<StoreComponent, ComponentInit>((_, c, _) => RefreshAllListings(c));
        SubscribeLocalEvent<StoreComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null || !TryComp<StoreComponent>(args.Target, out var store))
            return;

        args.Handled = TryAddCurrency(args.Used, args.Target.Value, component, store);
    }

    private void OnActivate(EntityUid uid, StoreComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleUi(args.User, component);
    }

    public bool TryAddCurrency(EntityUid? used, EntityUid receiver, CurrencyComponent currency, StoreComponent? store = null)
    {
        if (!Resolve(receiver, ref store))
            return false;

        var amount = 1;
        if (used != null)
        {
            TryComp<StackComponent>(used, out var stack);
            amount = stack != null ? stack.Count : 1;

            var msg = Loc.GetString("store-currency-inserted", ("used", used), ("target", receiver));
            _popup.PopupEntity(msg, receiver, Filter.Pvs(receiver));

            QueueDel(used.Value);
        }

        foreach (var type in currency.Price)
        {
            var adjustedValue = type.Value * amount;

            if (!store.Currency.TryAdd(type.Key, adjustedValue))
                store.Currency[type.Key] += adjustedValue;
        }

        return true;
    }
}
