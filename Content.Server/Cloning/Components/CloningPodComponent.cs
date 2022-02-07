using Content.Shared.Cloning;
using Robust.Shared.Containers;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : SharedCloningPodComponent
    {
        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind.Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;
        // Used to prevent as many duplicate UI messages as possible
        [ViewVariables] public bool UiKnownPowerState = false;

        [ViewVariables]
        public CloningPodStatus Status;
    }
}
