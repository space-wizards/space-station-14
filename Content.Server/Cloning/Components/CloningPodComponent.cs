using Content.Shared.Cloning;
using Robust.Shared.Containers;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : Component
    {
        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind.Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;
        [ViewVariables] public CloningPodStatus Status;
        public EntityUid? ConnectedConsole;

        /// <summary>
        ///     The port for cloning pods.
        /// </summary>
        [DataField("scannerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string PodPort = "CloningPodReceiver";
    }
}
