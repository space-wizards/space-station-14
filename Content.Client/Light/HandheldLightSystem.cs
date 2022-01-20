using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Client.Light.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using System.Linq;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : EntitySystem
{
    [Dependency] private readonly ItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldLightComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<HandheldLightComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) } );
        SubscribeLocalEvent<HandheldLightComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: new[] { typeof(ClothingSystem)});
    }

    /// <summary>
    ///     Add the unshaded light overlays to any clothing sprites.
    /// </summary>
    private void OnGetEquipmentVisuals(EntityUid uid, HandheldLightComponent component, GetEquipmentVisualsEvent args)
    {
        if (!component.Activated)
            return;

        if (!component.ClothingVisuals.TryGetValue(args.Slot, out var layers))
            return;

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-light" : $"{args.Slot}-light-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    /// <summary>
    ///     Add the unshaded light overlays to any in-hand sprites.
    /// </summary>
    private void OnGetHeldVisuals(EntityUid uid, HandheldLightComponent component, GetInhandVisualsEvent args)
    {
        if (!component.Activated)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-light";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    private void OnHandleState(EntityUid uid, HandheldLightComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedHandheldLightComponent.HandheldLightComponentState state)
            return;

        component.Level = state.Charge;

        if (state.Activated == component.Activated)
            return;

        component.Activated = state.Activated;
        _itemSys.VisualsChanged(uid);

        if (TryComp(component.Owner, out SpriteComponent? sprite))
        {
            sprite.LayerSetVisible(component.Layer, state.Activated);
        }

        if (TryComp(uid, out PointLightComponent? light))
        {
            light.Enabled = state.Activated;
        }

        // really hand-held lights should be using a separate unshaded layer. (see FlashlightVisualizer)
        // this prefix stuff is largely for backwards compatibility with RSIs/yamls that have not been updated.
        if (component.AddPrefix && TryComp(uid, out SharedItemComponent? item))
            item.EquippedPrefix = state.Activated ? "on" : "off";
    }
}
