using Content.Shared.Light;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PdaVisualizerSystem : VisualizerSystem<PdaVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PdaVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PdaVisuals.PdaType, out var pdaType, args.Component))
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PdaVisualLayers.Base, pdaType);

        if (AppearanceSystem.TryGetData<bool>(uid, UnpoweredFlashlightVisuals.LightOn, out var isFlashlightOn, args.Component))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PdaVisualLayers.Flashlight, isFlashlightOn);

        if (AppearanceSystem.TryGetData<bool>(uid, PdaVisuals.IdCardInserted, out var isCardInserted, args.Component))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PdaVisualLayers.IdLight, isCardInserted);
    }

    public enum PdaVisualLayers : byte
    {
        Base,
        Flashlight,
        IdLight
    }
}
