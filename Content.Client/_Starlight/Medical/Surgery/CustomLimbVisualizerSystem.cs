using Content.Shared.Starlight.ItemSwitch;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Humanoid;
using System;
using System.Numerics;
using Robust.Client.Graphics;
using Content.Shared.DisplacementMap;
using Content.Client.DisplacementMap;
using System.Reflection;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using Content.Client.Clothing;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Client._Starlight.Medical.Surgery;

public sealed class CustomLimbVisualizerSystem : EntitySystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomLimbVisualizerComponent, AfterAutoHandleStateEvent>(OnChanged);
    }

    private void OnChanged(Entity<CustomLimbVisualizerComponent> ent, ref AfterAutoHandleStateEvent _) => OnChanged(ent);
    private void OnChanged(Entity<CustomLimbVisualizerComponent> ent, bool repeat = true)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        var old = ent.Comp.CachedLayers.ToHashSet();
        ent.Comp.CachedLayers.Clear();

        foreach (var item in ent.Comp.Layers)
        {
            if (!item.Value.HasValue || !TryComp<SpriteComponent>(GetEntity(item.Value), out var layerSprite))
            {
                if (repeat) Timer.Spawn(TimeSpan.FromMilliseconds(150), () => OnChanged(ent, false));
                return;
            }
            string? state = null;
            if (TryComp<ItemComponent>(GetEntity(item.Value), out var itemComp) && itemComp.HeldPrefix is not null)
                state = $"{itemComp.HeldPrefix}-";

            var offset = Vector2.Zero;
            switch (item.Key)
            {
                case HumanoidVisualLayers.LArm:
                case HumanoidVisualLayers.LHand:
                case HumanoidVisualLayers.LLeg:
                case HumanoidVisualLayers.LFoot:
                    state += "inhand-left";
                    break;
                case HumanoidVisualLayers.RArm:
                case HumanoidVisualLayers.RHand:
                case HumanoidVisualLayers.RLeg:
                case HumanoidVisualLayers.RFoot:
                    state += "inhand-right";
                    break;
            }
            if (state is null) continue;

            switch (item.Key)
            {
                case HumanoidVisualLayers.LArm:
                    offset = new Vector2(0, 0.1875f);
                    break;
                case HumanoidVisualLayers.LHand:
                    offset = new Vector2(0, 0.09375f);
                    break;
                case HumanoidVisualLayers.LLeg:
                    offset = new Vector2(0, -0.15625f);
                    break;
                case HumanoidVisualLayers.LFoot:
                    offset = new Vector2(0, -0.34375f);
                    break;
                case HumanoidVisualLayers.RArm:
                    offset = new Vector2(0, 0.1875f);
                    break;
                case HumanoidVisualLayers.RHand:
                    offset = new Vector2(0, 0.09375f);
                    break;
                case HumanoidVisualLayers.RLeg:
                    offset = new Vector2(0, -0.15625f);
                    break;
                case HumanoidVisualLayers.RFoot:
                    offset = new Vector2(0, -0.34375f);
                    break;
            }
            if (layerSprite?.BaseRSI?.TryGetState(state, out var rsiState) ?? false)
            {
                var index = sprite.LayerMapReserveBlank($"custom-{item.Key}");

                sprite.LayerSetState(index, rsiState.StateId, layerSprite.BaseRSI);
                sprite.LayerSetOffset(index, offset);
                sprite.LayerSetVisible(index, true);
                ent.Comp.CachedLayers.Add(item.Key);
            }

            //if (ent.Comp.Displacements.TryGetValue(item.Key, out var displacementData) && !ent.Comp.CachedLayers.Contains($"{item.Key}-displacement"))
            //{
            //    sprite.LayerMapSet(item.Key.ToString(), (int)item.Key);
            //    _displacement.TryAddDisplacement(displacementData, sprite, (int)item.Key, item.Key.ToString(), ent.Comp.CachedLayers);
            //}
        }

        foreach (var layer in old)
            if (!ent.Comp.CachedLayers.Contains(layer))
            {
                var index = sprite.LayerMapReserveBlank($"custom-{layer}");
                sprite.LayerSetVisible(layer, false);
            }
    }
}
