using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent.MedicalScannerStatus;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    [UsedImplicitly]
    public class MedicalScannerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(MedicalScannerVisuals.Status, out MedicalScannerStatus status)) return;
            sprite.LayerSetState(MedicalScannerVisualLayers.Machine, StatusToMachineStateId(status));
            sprite.LayerSetState(MedicalScannerVisualLayers.Terminal, StatusToTerminalStateId(status));
        }

        private string StatusToMachineStateId(MedicalScannerStatus status)
        {
            switch (status)
            {
                case Off: return "closed";
                case Open: return "open";
                case Red: return "closed";
                case Death: return "closed";
                case Green: return "occupied";
                case Yellow: return "closed";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
            }
        }

        private string StatusToTerminalStateId(MedicalScannerStatus status)
        {
            switch (status)
            {
                case Off: return "off_unlit";
                case Open: return "idle_unlit";
                case Red: return "red_unlit";
                case Death: return "red_unlit";
                case Green: return "idle_unlit";
                case Yellow: return "maint_unlit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
            }
        }

        public enum MedicalScannerVisualLayers : byte
        {
            Machine,
            Terminal,
        }
    }
}
