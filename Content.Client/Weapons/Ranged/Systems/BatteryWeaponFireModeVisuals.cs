using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Systems;

/// <inheritdoc/>
public sealed class BatteryWeaponFireModesVisuals : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
    }

    private void OnAppearanceChange(Entity<BatteryWeaponFireModesComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<Color>(ent.Owner, BatteryWeaponFireModeVisualizer.Color, out var color, args.Component))
            return;

        if (TryComp(ent, out SpriteComponent? sprite) && _sprite.LayerExists((ent.Owner, sprite), BatteryWeaponFireModeVisualizer.Color))
                _sprite.LayerSetColor((ent.Owner, sprite), BatteryWeaponFireModeVisualizer.Color, color);

        _item.VisualsChanged(ent);

    }

    private void OnGetHeldVisuals(Entity<BatteryWeaponFireModesComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        if (!_appearance.TryGetData<Color>(ent.Owner, BatteryWeaponFireModeVisualizer.Color, out var color, appearance))
            return;

        if (!ent.Comp.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;


        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-color";

        if (TryComp(ent, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
        {
            if (!ent.Comp.WieldedInhandVisuals.TryGetValue(args.Location, out var wieldedLayers))
                return;
            AddLayers(wieldedLayers, color, defaultKey, args);
            return;
        }
        AddLayers(layers, color, defaultKey, args);
    }

    private void AddLayers(List<PrototypeLayerData> layers, Color color, string defaultKey, GetInhandVisualsEvent args)
    {
        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }
            layer.Color =  color;
            args.Layers.Add((key, layer));
        }
    }

    private void OnGetEquipmentVisuals(Entity<BatteryWeaponFireModesComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;
        List<PrototypeLayerData>? layers = null;

        // attempt to get species specific data
        if (inventory.SpeciesId != null)
            ent.Comp.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // No species specific data.  Try to default to generic data.
        if (layers == null && !ent.Comp.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        if (!_appearance.TryGetData<Color>(ent.Owner, BatteryWeaponFireModeVisualizer.Color, out var color, appearance))
            return;

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-color" : $"{args.Slot}-color-{i}";
                i++;
            }

            layer.Color = color;
            args.Layers.Add((key, layer));
        }
    }
}
