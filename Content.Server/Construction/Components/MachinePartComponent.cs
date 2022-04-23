namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed class MachinePartComponent : Component
    {
        // I'm so sorry for hard-coding this. But trust me, it should make things less painful.
        public static IReadOnlyDictionary<MachinePart, string> Prototypes { get; } = new Dictionary<MachinePart, string>()
        {
            {MachinePart.Capacitor, "CapacitorStockPart"},
            {MachinePart.ScanningModule, "ScanningModuleStockPart"},
            {MachinePart.Manipulator, "MicroManipulatorStockPart"},
            {MachinePart.Laser, "MicroLaserStockPart"},
            {MachinePart.MatterBin, "MatterBinStockPart"},
            {MachinePart.Ansible, "AnsibleSubspaceStockPart"},
            {MachinePart.Filter, "FilterSubspaceStockPart"},
            {MachinePart.Amplifier, "AmplifierSubspaceStockPart"},
            {MachinePart.Treatment, "TreatmentSubspaceStockPart"},
            {MachinePart.Analyzer, "AnalyzerSubspaceStockPart"},
            {MachinePart.Crystal, "CrystalSubspaceStockPart"},
            {MachinePart.Transmitter, "TransmitterSubspaceStockPart"}
        };
        [ViewVariables] [DataField("part")] public MachinePart PartType { get; private set; } = MachinePart.Capacitor;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rating")]
        public int Rating { get; private set; } = 1;
    }
}
