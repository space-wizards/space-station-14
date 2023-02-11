using Robust.Client.GameObjects;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;

namespace Content.Client.MedicalScanner;

public sealed class MedicalScannerSystem : VisualizerSystem<MedicalScannerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MedicalScannerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<MedicalScannerStatus>(uid, MedicalScannerVisuals.Status, out var status, args.Component))
            return;
        
        if (comp.MachineStateStatusMap.TryGetValue(status, out var machineState))
            args.Sprite.LayerSetState(MedicalScannerVisualLayers.Machine, machineState);
        if (comp.TerminalStateStatusMap.TryGetValue(status, out var terminalState))
            args.Sprite.LayerSetState(MedicalScannerVisualLayers.Terminal, terminalState);
    }
}

public enum MedicalScannerVisualLayers : byte
{
    Machine,
    Terminal,
}
