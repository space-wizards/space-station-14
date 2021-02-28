using Robust.Shared.GameObjects;
using Robust.Shared.Map;


namespace Content.Shared.GameObjects.Components.Movement
{
    public interface IMobMoverComponent : IComponent
    {
        EntityCoordinates LastPosition { get; set; }

        public float StepSoundDistance { get; set; }

        float GrabRange { get; set; }
    }
}
