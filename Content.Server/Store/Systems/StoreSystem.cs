using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ActivateInWorldEvent>(OnActivate);

        InitializeUi();
    }

    private void OnStartup(EntityUid uid, StoreComponent component, ComponentStartup args)
    {
        RefreshAllListings(component);
        InitializeFromPreset(component.Preset, component);
    }

    private void OnAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null || !TryComp<StoreComponent>(args.Target, out var store))
            return;

        args.Handled = TryAddCurrency(GetCurrencyValue(component), store);

        if (args.Handled)
        {
            var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", args.Target));
            _popup.PopupEntity(msg, args.Target.Value, Filter.Pvs(args.Target.Value));
            QueueDel(args.Used);
        }
    }

    public Dictionary<string, FixedPoint2> GetCurrencyValue(CurrencyComponent component)
    {
        TryComp<StackComponent>(component.Owner, out var stack);
        var amount = stack == null ? 1 : stack.Count;

        return component.Price.ToDictionary(v => v.Key, p => p.Value * amount);
    }

    public bool TryAddCurrency(CurrencyComponent component, StoreComponent store)
    {
        return TryAddCurrency(GetCurrencyValue(component), store);
    }

    public bool TryAddCurrency(Dictionary<string, FixedPoint2> currency, StoreComponent store)
    {
        //verify these before values are modified
        foreach (var type in currency)
        {
            if (!store.CurrencyWhitelist.Contains(type.Key))
                return false;
        }

        foreach (var type in currency)
        {
            if (!store.Balance.TryAdd(type.Key, type.Value))
                store.Balance[type.Key] += type.Value;
        }

        UpdateUserInterface(null, store);
        return true;
    }

    private void OnActivate(EntityUid uid, StoreComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleUi(args.User, component);
    }

    public void InitializeFromPreset(string? preset, StoreComponent component)
    {
        if (preset == null)
            return;
            

        if (!_proto.TryIndex<StorePresetPrototype>(preset, out var proto))
            return;

        InitializeFromPreset(proto, component);
    }

    public void InitializeFromPreset(StorePresetPrototype preset, StoreComponent component)
    {
        component.CurrencyWhitelist.UnionWith(preset.CurrencyWhitelist);
        component.ActivateInHand = preset.ActivateInHand;
        component.Categories.UnionWith(preset.Categories);
        if (component.Balance == new Dictionary<string, FixedPoint2>()) //if we don't have a value stored, use the preset
            TryAddCurrency(preset.InitialBalance, component);
    }
}
