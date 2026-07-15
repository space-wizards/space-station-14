using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;

namespace Content.Client.SprayPainter;

/// <summary>
/// Updates an object's layer zero sprite whenever it is spraypainted.
/// </summary>
public sealed partial class PaintableVisualizerSystem : VisualizerSystem<PaintableVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PaintableVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        if (!ProtoMan.HasIndex(protoName))
            return;

        // Create the given prototype and get its first layer.
        var tempUid = Spawn(protoName);
        SpriteSystem.LayerSetRsiState(uid, 0, SpriteSystem.LayerGetRsiState(tempUid, 0));
        QueueDel(tempUid);
    }
}
