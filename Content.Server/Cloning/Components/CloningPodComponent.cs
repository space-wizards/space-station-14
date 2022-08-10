using Content.Shared.Cloning;
using Robust.Shared.Containers;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : Component
    {
        public const string PodPort = "CloningPodReceiver";
        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind.Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;
        [ViewVariables] public CloningPodStatus Status;
        public EntityUid? ConnectedConsole;
    }
}
