using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.SprayPainter.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasCanisterAppearanceSystem : VisualizerSystem<GasCanisterComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GasCanisterComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.BaseRSI, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        var proto = Spawn(protoName);

        if (!TryComp<SpriteComponent>(proto, out var sprite))
            return;

        old.LayerSetState(0, sprite.LayerGetState(0));

        Del(proto);
    }
}
