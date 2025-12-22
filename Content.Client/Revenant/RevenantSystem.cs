using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed class RevenantSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<RevenantComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnAppearanceChange(EntityUid uid, RevenantComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Harvesting, out var harvesting, args.Component) && harvesting)
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), 0, component.HarvestingState);
        }
        else if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Stunned, out var stunned, args.Component) && stunned)
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), 0, component.StunnedState);
        }
        else if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Corporeal, out var corporeal, args.Component))
        {
            if (corporeal)
                _sprite.LayerSetRsiState((uid, args.Sprite), 0, component.CorporealState);
            else
                _sprite.LayerSetRsiState((uid, args.Sprite), 0, component.State);
        }
    }

    private void OnGetCounterAmount(Entity<RevenantComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.EssenceAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Essence.Int();
    }
}
