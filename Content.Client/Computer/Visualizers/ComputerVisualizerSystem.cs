using Content.Client.Wires.Visualizers;
using Content.Shared.Computer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Computer.Visualizers;

/// <summary>
/// A visualizer used to draw the different states of computers.
/// </summary>
/// <remarks>
/// Helps reduce YAML redundancy in computer sprite definitions.
/// </remarks>
/// <seealso cref="ComputerVisualsComponent"/>
public sealed partial class ComputerVisualizerSystem : VisualizerSystem<ComputerVisualsComponent>
{
    [Dependency] EntityQuery<SpriteComponent> _spriteQuery;

    private ShaderInstance _unshadedShader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _unshadedShader = ProtoMan.Index(SpriteSystem.UnshadedId).Instance();
    }

    protected override void OnAppearanceChange(EntityUid uid,
        ComputerVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<bool>(ComputerVisuals.Powered, out var powered))
            return;

        // Need to get the index first because the mapped LayerSetShader doesn't accept null shader instances.
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ComputerVisualLayers.Screen, out var screenLayer, logMissing: false))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), screenLayer, powered);
            args.Sprite.LayerSetShader(screenLayer, powered ? _unshadedShader : null);
        }

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ComputerVisualLayers.Keys, out var keysLayer, logMissing: false))
        {
            args.Sprite.LayerSetShader(keysLayer, powered ? _unshadedShader : null);
        }
    }

    /// <summary>
    /// Sets up the sprite from this component's state. Exists to reduce computer sprite boilerplate.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<ComputerVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (ent, sprite);

        TrySetLayerState(spriteEnt, ComputerVisualLayers.Frame, ent.Comp.StateFrame);
        TrySetLayerState(spriteEnt, ComputerVisualLayers.Keyboard, ent.Comp.StateKeyboard);
        TrySetLayerState(spriteEnt, ComputerVisualLayers.Keys, ent.Comp.StateKeys);
        TrySetLayerState(spriteEnt, ComputerVisualLayers.Screen, ent.Comp.StateScreen);
        TrySetLayerState(spriteEnt, WiresVisualLayers.MaintenancePanel, ent.Comp.StatePanel);
    }

    private void TrySetLayerState(Entity<SpriteComponent?> ent, Enum key, string? state)
    {
        if (SpriteSystem.LayerMapTryGet(ent, key, out var layerIndex, logMissing: false))
            SpriteSystem.LayerSetRsiState(ent, layerIndex, state);
    }
}

/// <summary>
/// The set of visual layers used for computer visualizations.
/// </summary>
public enum ComputerVisualLayers : byte
{
    Frame,
    Keyboard,
    Keys,
    Screen
}
