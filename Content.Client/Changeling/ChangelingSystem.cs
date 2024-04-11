using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;

namespace Content.Client.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<ChangelingComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.AlertType != AlertType.Chemicals)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var chemicals = Math.Clamp(ent.Comp.Chemicals, 0, 999);
        sprite.LayerSetState(ChangelingVisualLayers.Digit1, $"{(chemicals / 100) % 10}");
        sprite.LayerSetState(ChangelingVisualLayers.Digit2, $"{(chemicals / 10) % 10}");
        sprite.LayerSetState(ChangelingVisualLayers.Digit3, $"{chemicals % 10}");
    }
}