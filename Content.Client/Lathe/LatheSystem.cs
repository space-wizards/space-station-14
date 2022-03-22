using Robust.Client.GameObjects;
using Content.Shared.Lathe;

namespace Content.Client.Lathe
{
    public sealed class LatheSystem : VisualizerSystem<LatheVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, LatheVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(LatheVisuals.IsOn, out bool isOn)
                && args.Component.TryGetData(LatheVisuals.IsRunning, out bool isRunning)
                && args.Component.TryGetData(LatheVisuals.PanelOpen, out bool panelOpen))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                sprite.LayerSetVisible(LatheVisualLayers.IsOn, isOn);
                sprite.LayerSetVisible(LatheVisualLayers.PanelOpen, panelOpen);
                sprite.LayerSetState(LatheVisualLayers.IsRunning, state);
                // More specific inserting stuff
                if (component.HasInsertingAnims && args.Component.TryGetData(LatheVisuals.IsInserting, out bool isInserting))
                {
                    sprite.LayerSetVisible(LatheVisualLayers.IsInserting, isInserting);
                }
            }
        }
    }
}
public enum LatheVisualLayers : byte
{
    IsOn,
    IsRunning,
    IsInserting,
    PanelOpen
}
