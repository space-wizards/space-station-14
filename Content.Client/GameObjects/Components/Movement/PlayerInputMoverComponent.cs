#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class PlayerInputMoverComponent : SharedPlayerInputMoverComponent
    {
        public override EntityCoordinates LastPosition { get; set; }
        public override float StepSoundDistance { get; set; }
    }
}
