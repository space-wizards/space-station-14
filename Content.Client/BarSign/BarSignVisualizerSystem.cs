using Content.Shared.BarSign;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.BarSign;

public sealed partial class BarSignVisualizerSystem : VisualizerSystem<BarSignComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BarSignComponent component, ref AppearanceChangeEvent args)
    {
        args.TryGetData<bool>(PowerDeviceVisuals.Powered, out var powered);
        args.TryGetData<string>(BarSignVisuals.BarSignPrototype, out var currentSign);

        if (powered
            && currentSign != null
            && ProtoMan.Resolve<BarSignPrototype>(currentSign, out var proto))
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
