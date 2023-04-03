using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class VisionCorrectionComponent : Component
    {
        [ViewVariables]
        public bool IsActive = false;

        [DataField("visionBonus")]
        public float VisionBonus = 3f;
    }
}
