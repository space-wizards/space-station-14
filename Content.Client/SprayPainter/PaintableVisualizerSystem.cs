using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SprayPainter;

/// <summary>
/// Updates an object's layer zero sprite whenever it is spraypainted.
/// </summary>
public sealed partial class PosterVisualizerSystem : VisualizerSystem<PaintableVisualizerComponent>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    protected override void OnAppearanceChange(EntityUid uid, PaintableVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        if (!_prototypeManager.HasIndex(protoName))
            return;

        // Create the given prototype and get its first layer.
        var tempUid = Spawn(protoName);
        SpriteSystem.LayerSetRsiState(uid, 0, SpriteSystem.LayerGetRsiState(tempUid, 0));
        QueueDel(tempUid);
    }
}
