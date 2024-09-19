using Content.Client.Alerts;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;

namespace Content.Client.Revenant;

public sealed class RevenantRegenModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantRegenModifierComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<RevenantRegenModifierComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.Alert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var witnesses = Math.Clamp(ent.Comp.Witnesses.Count, 0, 99);
        sprite.LayerSetState(RevenantVisualLayers.Digit1, $"{witnesses / 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit2, $"{witnesses % 10}");
    }
}