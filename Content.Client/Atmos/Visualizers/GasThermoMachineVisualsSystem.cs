using Robust.Client.GameObjects;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Wires;
using Content.Client.Wires.Visualizers;

namespace Content.Client.Atmos.Visualizers
{   /// <summary>
    /// Controls client-side visuals for gas thermomachines (freezers/heaters).
    /// </summary>
    public sealed class GasThermoMachineSystem : VisualizerSystem<GasThermoMachineVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, GasThermoMachineVisualsComponent visuals, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;


            args.AppearanceData.TryGetValue(ThermoMachineVisuals.Running, out var isRunningObject);
            args.Sprite.LayerSetState(GasThermoMachineVisualLayers.MachineBody, isRunningObject is true ? visuals.OnState : visuals.OffState);

            if (!args.AppearanceData.TryGetValue(WiresVisuals.MaintenancePanelState, out var panelOpen))
            {
                args.Sprite.LayerSetState(WiresVisualLayers.MaintenancePanel, visuals.PanelClose);
                return;
            }

            if (panelOpen is true)
            {
                args.Sprite.LayerSetState(WiresVisualLayers.MaintenancePanel, visuals.PanelOpen);
            }
            else
            {
                args.Sprite.LayerSetState(WiresVisualLayers.MaintenancePanel, isRunningObject is true ? visuals.PanelOn : visuals.PanelClose);
            }

        }

    }

    public enum GasThermoMachineVisualLayers : byte
    {
        MachineBody,
    }

}


