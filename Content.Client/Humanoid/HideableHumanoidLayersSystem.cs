using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client.Humanoid;

public sealed class HideableHumanoidLayersSystem : SharedHideableHumanoidLayersSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HideableHumanoidLayersComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HideableHumanoidLayersComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnComponentInit(Entity<HideableHumanoidLayersComponent> ent, ref ComponentInit args)
    {
        UpdateSprite(ent);
    }

    private void OnHandleState(Entity<HideableHumanoidLayersComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    public override void SetLayerOcclusion(
        Entity<HideableHumanoidLayersComponent?> ent,
        HumanoidVisualLayers layer,
        bool visible,
        SlotFlags source)
    {
        base.SetLayerOcclusion(ent, layer, visible, source);

        if (Resolve(ent, ref ent.Comp))
            UpdateSprite((ent, ent.Comp));
    }

    private void UpdateSprite(Entity<HideableHumanoidLayersComponent> ent)
    {
        foreach (var item in ent.Comp.LastHiddenLayers)
        {
            if (ent.Comp.HiddenLayers.ContainsKey(item))
                continue;

            var evt = new HumanoidLayerVisibilityChangedEvent(item, true);
            RaiseLocalEvent(ent, ref evt);

            if (!_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, true);
        }

        foreach (var item in ent.Comp.HiddenLayers.Keys)
        {
            if (ent.Comp.LastHiddenLayers.Contains(item))
                continue;

            var evt = new HumanoidLayerVisibilityChangedEvent(item, false);
            RaiseLocalEvent(ent, ref evt);

            if (!_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, false);
        }

        ent.Comp.LastHiddenLayers.Clear();
        ent.Comp.LastHiddenLayers.UnionWith(ent.Comp.HiddenLayers.Keys);
    }
}
