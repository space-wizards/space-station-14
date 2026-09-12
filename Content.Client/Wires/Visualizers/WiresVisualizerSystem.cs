using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Wires.Visualizers;

/// <summary>
/// A system that automatically updates sprites for entities with maintenance panels.
/// </summary>
public sealed partial class WiresVisualizerSystem : VisualizerSystem<WiresVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, WiresVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var layer = SpriteSystem.LayerMapReserve((uid, args.Sprite), WiresVisualLayers.MaintenancePanel);

        // Data doesn't exist (e.g. in the spawn menu), act as though the panel's closed
        if (!args.AppearanceData.TryGetValue(WiresVisuals.MaintenancePanelState, out var panelOpenObj)
            || panelOpenObj is not bool panelOpen)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, component.VisibleWhenClosed);
            return;
        }

        // Otherwise, set the visibility according to when it's closed.
        var visible = component.VisibleWhenClosed ? !panelOpen : panelOpen;
        SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, visible);
    }
}

/// <summary>
/// Layers for the maintenance panel.
/// </summary>
public enum WiresVisualLayers : byte
{
    MaintenancePanel
}
