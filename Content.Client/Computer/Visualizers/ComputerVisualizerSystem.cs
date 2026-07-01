using Content.Client.Wires.Visualizers;
using Content.Shared.Computer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Computer.Visualizers;

/// <summary>
/// A visualizer used to draw the different states of computers.
/// Helps reduce YAML redundancy in computer sprite definitions.
/// </summary>
public sealed partial class ComputerVisualizerSystem : VisualizerSystem<ComputerVisualsComponent>
{
    private ShaderInstance _unshadedShader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _unshadedShader = ProtoMan.Index(SpriteSystem.UnshadedId).Instance();

        SubscribeLocalEvent<ComputerVisualsComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Sets the base sprite to this layer. Exists to reduce computer sprite boilerplate.
    /// </summary>
    private void OnComponentInit(Entity<ComputerVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (ent, sprite);

        SpriteSystem.LayerSetRsiState(spriteEnt, ComputerVisualLayers.Frame, ent.Comp.StateFrame);
        SpriteSystem.LayerSetRsiState(spriteEnt, ComputerVisualLayers.Keyboard, ent.Comp.StateKeyboard);
        SpriteSystem.LayerSetRsiState(spriteEnt, ComputerVisualLayers.Keys, ent.Comp.StateKeys);
        SpriteSystem.LayerSetRsiState(spriteEnt, ComputerVisualLayers.Screen, ent.Comp.StateScreen);
        SpriteSystem.LayerSetRsiState(spriteEnt, WiresVisualLayers.MaintenancePanel, ent.Comp.StatePanel);
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
