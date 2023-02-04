using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles;

/// <summary>
/// Handles making a thruster visibly turn on/emit an exhaust plume according to its state. 
/// </summary>
public sealed class ThrusterVisualizerSystem : VisualizerSystem<ThrusterVisualsComponent>
{
    /// <summary>
    /// Updates whether or not the thruster is visibly active/thrusting.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, ThrusterVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.State, out var state, args.Component))
            return;

        switch (state)
        {
            case true:
                args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustOn, true);

                if (AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.Thrusting, out var thrusting, args.Component) && thrusting)
                {
                    SetThrusting(uid, true, args.Component, args.Sprite);
                }
                else
                {
                    SetThrusting(uid, false, args.Component, args.Sprite);
                }

                break;
            case false:
                args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustOn, false);
                SetThrusting(uid, true, args.Component, args.Sprite);
                break;
        }
    }

    /// <summary>
    /// Sets whether or not the exhaust plume of the thruster is visible or not.
    /// </summary>
    private void SetThrusting(EntityUid uid, bool value, AppearanceComponent appearance, SpriteComponent sprite)
    {
        if (sprite.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out var thrustingLayer))
        {
            sprite.LayerSetVisible(thrustingLayer, value);
        }

        if (sprite.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out var unshadedLayer))
        {
            sprite.LayerSetVisible(unshadedLayer, value);
        }
    }
}

public enum ThrusterVisualLayers : byte
{
    Base,
    ThrustOn,
    Thrusting,
    ThrustingUnshaded,
}
