using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningConsoleComponent : Component
    {
        [ViewVariables]
        public EntityUid? GeneticScanner = null;
        [ViewVariables]
        public EntityUid? CloningPod = null;

        /// <summary>
        ///     The port for medical scanners.
        /// </summary>
        [DataField("scannerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string ScannerPort = "MedicalScannerSender";

        /// <summary>
        ///     The port for cloning pods.
        /// </summary>
        [DataField("podPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string PodPort = "CloningPodSender";

        public bool Powered = false;
    }
}
