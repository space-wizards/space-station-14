using Content.Shared.Cloning;
using Robust.Shared.Containers;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : SharedCloningPodComponent
    {
        [ViewVariables]
        public ContainerSlot BodyContainer = default!;
        [ViewVariables]
        public Mind.Mind? CapturedMind;
        [ViewVariables]
        public float CloningProgress = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cloningTime")]
        public float CloningTime = 60f;

        [ViewVariables]
        public CloningPodStatus Status;
    }
}
