using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client.Humanoid;

/// <inheritdoc />
public sealed partial class HideableHumanoidLayersSystem : SharedHideableHumanoidLayersSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<VisualOrganComponent> _visualOrganQuery = default!;

    /// <inheritdoc />
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

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<HideableHumanoidLayersComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<HideableHumanoidLayersComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (_visualOrganQuery.HasComp(args.Organ))
            UpdateSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<HideableHumanoidLayersComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (_visualOrganQuery.HasComp(args.Organ))
            UpdateSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<HideableHumanoidLayersComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void UpdateSprite(Entity<HideableHumanoidLayersComponent> ent)
    {
        foreach (var item in ent.Comp.LastHiddenLayers)
        {
            if (ent.Comp.HiddenLayers.ContainsKey(item))
                continue;

            var evt = new HumanoidLayerVisibilityChangedEvent(item, true);
            RaiseLocalEvent(ent, ref evt);

            if (!evt.Handled || !_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, true);
        }

        var actualHiddenLayers = new List<HumanoidVisualLayers>(ent.Comp.HiddenLayers.Count);
        foreach (var item in ent.Comp.HiddenLayers.Keys)
        {
            if (ent.Comp.LastHiddenLayers.Contains(item))
            {
                actualHiddenLayers.Add(item);
                continue;
            }

            var evt = new HumanoidLayerVisibilityChangedEvent(item, false);
            RaiseLocalEvent(ent, ref evt);

            if (!evt.Handled || !_sprite.LayerMapTryGet(ent.Owner, item, out var index, true))
                continue;

            _sprite.LayerSetVisible(ent.Owner, index, false);
            actualHiddenLayers.Add(item);
        }

        ent.Comp.LastHiddenLayers = actualHiddenLayers;
    }
}
