using Content.Client.Wires.Visualizers;
using Content.Shared.Computer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Computer.Visualizers;

/// <summary>
/// A visualizer used to draw the different states of computers.
/// Helps reduce YAML redundancy.
/// </summary>
public sealed partial class ComputerVisualizerSystem : VisualizerSystem<ComputerVisualsComponent>
{
    private ShaderInstance _unshadedShader = default!;

    public override void Initialize()
    {
        base.Initialize();
        _unshadedShader = ProtoMan.Index(SpriteSystem.UnshadedId).Instance();
    }

    /// <summary>
    /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentInit(EntityUid uid, ComputerVisualsComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        SpriteSystem.LayerSetRsiState((uid, sprite), ComputerVisualLayers.Frame, comp.StateFrame);
        SpriteSystem.LayerSetRsiState((uid, sprite), ComputerVisualLayers.Keyboard, comp.StateKeyboard);
        SpriteSystem.LayerSetRsiState((uid, sprite), ComputerVisualLayers.Keys, comp.StateKeys);
        SpriteSystem.LayerSetRsiState((uid, sprite), ComputerVisualLayers.Screen, comp.StateScreen);
        SpriteSystem.LayerSetRsiState((uid, sprite), WiresVisualLayers.MaintenancePanel, comp.StatePanel);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        ComputerVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, ComputerVisuals.Powered, out var powered, args.Component))
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
}

/// <summary>
/// The set of visual layers used for computer visualizations.
/// </summary>
public enum ComputerVisualLayers : byte
{
    Frame,
    Keys,
    Keyboard,
    Screen
}
