using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed class RevenantSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<RevenantComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnAppearanceChange(EntityUid uid, RevenantComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Harvesting, out var harvesting, args.Component) && harvesting)
        {
            args.Sprite.LayerSetState(0, component.HarvestingState);
        }
        else if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Stunned, out var stunned, args.Component) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (_appearance.TryGetData<bool>(uid, RevenantVisuals.Corporeal, out var corporeal, args.Component))
        {
            if (corporeal)
                args.Sprite.LayerSetState(0, component.CorporealState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }

    private void OnUpdateAlert(Entity<RevenantComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.AlertType != AlertType.Essence)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var essence = Math.Clamp(ent.Comp.Essence.Int(), 0, 999);
        sprite.LayerSetState(RevenantVisualLayers.Digit1, $"{(essence / 100) % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit2, $"{(essence / 10) % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit3, $"{essence % 10}");
    }
}
