using System;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent.MedicalScannerStatus;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    public class MedicalScannerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.Deleted)
            {
                return;
            }

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(MedicalScannerVisuals.Status, out MedicalScannerStatus status)) return;
            sprite.LayerSetState(MedicalScannerVisualLayers.Machine, StatusToMachineStateId(status));
            sprite.LayerSetState(MedicalScannerVisualLayers.Terminal, StatusToTerminalStateId(status));
        }

        private string StatusToMachineStateId(MedicalScannerStatus status)
        {
            switch (status)
            {
                case Off: return "scanner_off";
                case Open: return "scanner_open";
                case Red: return "scanner_red";
                case Death: return "scanner_death";
                case Green: return "scanner_green";
                case Yellow: return "scanner_yellow";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
            }
        }

        private string StatusToTerminalStateId(MedicalScannerStatus status)
        {
            switch (status)
            {
                case Off: return "scanner_terminal_off";
                case Open: return "scanner_terminal_blue";
                case Red: return "scanner_terminal_red";
                case Death: return "scanner_terminal_dead";
                case Green: return "scanner_terminal_green";
                case Yellow: return "scanner_terminal_blue";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
            }
        }

        public enum MedicalScannerVisualLayers
        {
            Machine,
            Terminal,
        }
    }
}
