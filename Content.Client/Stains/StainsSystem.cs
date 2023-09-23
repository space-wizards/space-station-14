using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Stains;
using Robust.Client.GameObjects;

namespace Content.Client.Stains;

public sealed class StainsSystem : SharedStainsSystem
{
    [Dependency] private readonly ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<StainableComponent, EquipmentVisualsUpdatedEvent>(OnVisualsUpdated);
    }

    private void OnAfterState(EntityUid uid, StainableComponent component, ref AfterAutoHandleStateEvent @event)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }
        sprite.Color = component.StainColor;
        _item.VisualsChanged(uid);
    }

    private void OnVisualsUpdated(EntityUid uid, StainableComponent component, EquipmentVisualsUpdatedEvent @event)
    {
        if (!TryComp<SpriteComponent>(@event.Equipee, out var sprite))
        {
            return;
        }
        foreach (var layer in @event.RevealedLayers)
        {
            sprite.LayerSetColor(layer, component.StainColor);
        }
    }
}
