using Content.Shared.Atmos.Components;
using Content.Shared.SprayPainter.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasCanisterAppearanceSystem : VisualizerSystem<GasCanisterComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GasCanisterComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.BaseRSI, out var protoName, args.Component))
            return;

        if (args.Sprite is not { } old)
            return;

        var proto = Spawn(protoName);

        if (!TryComp<SpriteComponent>(proto, out var sprite))
            return;

        foreach (var layer in old.AllLayers)
            layer.Rsi = sprite.BaseRSI;

        Del(proto);
    }
}
