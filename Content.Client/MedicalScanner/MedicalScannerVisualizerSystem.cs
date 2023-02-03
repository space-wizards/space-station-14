using Robust.Client.GameObjects;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent.MedicalScannerStatus;

namespace Content.Client.MedicalScanner;

public sealed class MedicalScannerVisualizerSystem : VisualizerSystem<MedicalScannerVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MedicalScannerVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData(uid, MedicalScannerVisuals.Status, out MedicalScannerStatus status, args.Component))
            return;
        
        args.Sprite.LayerSetState(MedicalScannerVisualLayers.Machine, StatusToMachineStateId(status));
        args.Sprite.LayerSetState(MedicalScannerVisualLayers.Terminal, StatusToTerminalStateId(status));
    }

    private string StatusToMachineStateId(MedicalScannerStatus status)
    {
        switch (status)
        {
            case Off: return "closed";
            case Open: return "open";
            case Red: return "occupied";
            case Death: return "occupied";
            case Green: return "occupied";
            case Yellow: return "occupied";
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
            case Death: return "off_unlit";
            case Green: return "idle_unlit";
            case Yellow: return "maint_unlit";
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
        }
    }
}

public enum MedicalScannerVisualLayers : byte
{
    Machine,
    Terminal,
}
