using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles;

/// <summary>
/// Handles making a thruster visibly turn on/emit an exhaust plume according to its state.
/// </summary>
public sealed class ThrusterSystem : VisualizerSystem<ThrusterComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    /// Updates whether or not the thruster is visibly active/thrusting.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, ThrusterComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
        || !AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.State, out var state, args.Component))
            return;

        _sprite.LayerSetVisible((uid, args.Sprite), ThrusterVisualLayers.ThrustOn, state);
        SetThrusting(
            uid,
            state && AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.Thrusting, out var thrusting, args.Component) && thrusting,
            args.Sprite
        );
    }

    /// <summary>
    /// Sets whether or not the exhaust plume of the thruster is visible or not.
    /// </summary>
    private void SetThrusting(EntityUid uid, bool value, SpriteComponent sprite)
    {
        if (_sprite.LayerMapTryGet((uid, sprite), ThrusterVisualLayers.Thrusting, out var thrustingLayer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), thrustingLayer, value);
        }

        if (_sprite.LayerMapTryGet((uid, sprite), ThrusterVisualLayers.ThrustingUnshaded, out var unshadedLayer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), unshadedLayer, value);
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
