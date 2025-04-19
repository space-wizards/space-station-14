using Content.Shared.Atmos.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasCanisterAppearanceSystem : VisualizerSystem<GasCanisterComponent>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    protected override void OnAppearanceChange(EntityUid uid, GasCanisterComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Canister, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        if (!_prototypeManager.TryIndex(protoName, out var proto))
            return;

        if (!proto.TryGetComponent(out SpriteComponent? sprite, _componentFactory))
            return;

        old.LayerSetState(0, sprite.LayerGetState(0));
    }
}
