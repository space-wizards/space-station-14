using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;

namespace Content.Client.Lathe
{
    public sealed class LatheSystem : VisualizerSystem<LatheVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, LatheVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered))
            {
                sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
            }

        }
    }
}
public enum LatheVisualLayers : byte
{
    IsRunning,
    IsInserting,
    PanelOpen
}
