using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent.MedicalScannerStatus;

namespace Content.Client.MedicalScanner
{
    [UsedImplicitly]
    public sealed class MedicalScannerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
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
