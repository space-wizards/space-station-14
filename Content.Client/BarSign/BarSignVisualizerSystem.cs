using Content.Shared.BarSign;
using Content.Shared.Power;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.BarSign;

public sealed class BarSignVisualizerSystem : VisualizerSystem<BarSignComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected override void OnAppearanceChange(EntityUid uid, BarSignComponent component, ref AppearanceChangeEvent args)
    {
        AppearanceSystem.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var powered, args.Component);
        AppearanceSystem.TryGetData<string>(uid, BarSignVisuals.BarSignPrototype, out var currentSign, args.Component);

        if (powered
            && currentSign != null
            && _prototypeManager.Resolve<BarSignPrototype>(currentSign, out var proto))
        {
            SpriteSystem.LayerSetSprite((uid, args.Sprite), 0, proto.Icon);
            args.Sprite?.LayerSetShader(0, "unshaded");
        }
        else
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, "empty");
            args.Sprite?.LayerSetShader(0, null, null);
        }
    }
}
