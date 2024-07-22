using Robust.Shared.Prototypes;

namespace Content.Server.Engineering.Components
{
    [RegisterComponent]
    public sealed partial class SpawnAfterInteractComponent : Component
    {
        [DataField]
        public EntProtoId? Prototype { get; private set; }

        [DataField("ignoreDistance")]
        public bool IgnoreDistance { get; private set; }

        [DataField("doAfter")]
        public float DoAfterTime = 0;

        [DataField("removeOnInteract")]
        public bool RemoveOnInteract = false;
    }
}
