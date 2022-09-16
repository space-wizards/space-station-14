using Content.Shared.Medical.Cryogenics;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Medical.Cryogenics;

public sealed class CryoPodSystem: VisualizerSystem<CryoPodVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CryoPodVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite)
            || !args.Component.TryGetData(SharedCryoPodComponent.CryoPodVisuals.IsOpen, out bool isOpen)
            || !args.Component.TryGetData(SharedCryoPodComponent.CryoPodVisuals.IsOn, out bool isOn))
            return;
        if (isOpen)
        {
            sprite.LayerSetState(CryoPodVisualLayers.Base, "pod-open");
            sprite.LayerSetVisible(CryoPodVisualLayers.Cover, false);
            sprite.DrawDepth = (int) DrawDepth.Objects;
        }
        else
        {
            sprite.DrawDepth = (int) DrawDepth.Mobs;
            sprite.LayerSetState(CryoPodVisualLayers.Base, isOn ? "pod-on" : "pod-off");
            sprite.LayerSetState(CryoPodVisualLayers.Cover, isOn ? "cover-on" : "cover-off");
            sprite.LayerSetVisible(CryoPodVisualLayers.Cover, true);
        }
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
