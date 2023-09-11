using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Toggleable;

public sealed class ToggleableLightVisualsSystem : VisualizerSystem<ToggleableLightVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableLightVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
        SubscribeLocalEvent<ToggleableLightVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: new[] { typeof(ClientClothingSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, ToggleableLightVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, ToggleableLightVisuals.Enabled, out var enabled, args.Component))
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleableLightVisuals.Color, out var color, args.Component);

        // Update the item's sprite
        if (args.Sprite != null && args.Sprite.LayerMapTryGet(component.SpriteLayer, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, enabled);
            if (modulate)
                args.Sprite.LayerSetColor(layer, color);
        }

        // Update any point-lights
        if (TryComp(uid, out PointLightComponent? light))
        {
            DebugTools.Assert(!light.NetSyncEnabled, "light visualizers require point lights without net-sync");
            _lights.SetEnabled(uid, enabled, light);
            if (enabled && modulate)
            {
                _lights.SetColor(uid, color, light);
            }
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    /// <summary>
    ///     Add the unshaded light overlays to any clothing sprites.
    /// </summary>
    private void OnGetEquipmentVisuals(EntityUid uid, ToggleableLightVisualsComponent component, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleableLightVisuals.Enabled, out var enabled, appearance)
            || !enabled)
            return;

        if (!component.ClothingVisuals.TryGetValue(args.Slot, out var layers))
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleableLightVisuals.Color, out var color, appearance);

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-toggle" : $"{args.Slot}-toggle-{i}";
                i++;
            }

            if (modulate)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }

    private void OnGetHeldVisuals(EntityUid uid, ToggleableLightVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleableLightVisuals.Enabled, out var enabled, appearance)
            || !enabled)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleableLightVisuals.Color, out var color, appearance);

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-toggle";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            if (modulate)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }
}
