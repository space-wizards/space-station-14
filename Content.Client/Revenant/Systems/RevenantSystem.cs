using Content.Client.Alerts;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;

namespace Content.Client.Revenant.Systems;

public sealed class RevenantSystem : SharedRevenantSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private static void OnUpdateAlert(Entity<RevenantComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.EssenceAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var essence = Math.Clamp(ent.Comp.Essence.Int(), 0, 999);
        sprite.LayerSetState(RevenantVisualLayers.Digit1, $"{essence / 100 % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit2, $"{essence / 10 % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit3, $"{essence % 10}");
    }
}
