namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed partial class CloningConsoleComponent : Component
    {
        public const string ScannerPort = "MedicalScannerSender";

        public const string PodPort = "CloningPodSender";

        [ViewVariables]
        public EntityUid? GeneticScanner = null;

        [ViewVariables]
        public EntityUid? CloningPod = null;

        /// Maximum distance between console and one if its machines
        [DataField("maxDistance")]
        public float MaxDistance = 4f;

        public bool GeneticScannerInRange = true;

        public bool CloningPodInRange = true;
    }
}
