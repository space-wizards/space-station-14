using Content.Server.EUI;
using Content.Server.Power.Components;
using Content.Shared.Cloning;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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
        [ViewVariables]
        public CloningPodStatus Status;
    }
}
