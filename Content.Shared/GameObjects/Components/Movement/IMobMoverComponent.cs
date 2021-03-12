using Robust.Shared.GameObjects;
using Robust.Shared.Map;


namespace Content.Shared.GameObjects.Components.Movement
{
    public interface IMobMoverComponent : IComponent
    {
        const float GrabRangeDefault = 0.6f;
        const float PushStrengthDefault = 600.0f;
        const float WeightlessStrengthDefault = 0.4f;

        EntityCoordinates LastPosition { get; set; }

        public float StepSoundDistance { get; set; }

        float GrabRange { get; set; }

        float PushStrength { get; set; }

        float WeightlessStrength { get; set; }
    }
}
