using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.GeneticScanner.SharedGeneticScannerComponent;
using static Content.Shared.GeneticScanner.SharedGeneticScannerComponent.GeneticScannerStatus;

namespace Content.Client.GeneticScanner
{
    [UsedImplicitly]
    public sealed class GeneticScannerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            if (!component.TryGetData(GeneticScannerVisuals.Status, out GeneticScannerStatus status)) return;
            sprite.LayerSetState(GeneticScannerVisualLayers.Machine, StatusToMachineStateId(status));
            sprite.LayerSetState(GeneticScannerVisualLayers.Terminal, StatusToTerminalStateId(status));
        }

        private string StatusToMachineStateId(GeneticScannerStatus status)
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
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown GeneticScannerStatus");
            }
        }

        private string StatusToTerminalStateId(GeneticScannerStatus status)
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
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown GeneticScannerStatus");
            }
        }

        public enum GeneticScannerVisualLayers : byte
        {
            Machine,
            Terminal,
        }
    }
}
