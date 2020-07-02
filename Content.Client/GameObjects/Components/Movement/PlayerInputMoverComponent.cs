using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

#nullable enable

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class PlayerInputMoverComponent : SharedPlayerInputMoverComponent, IMoverComponent
    {
        public override GridCoordinates LastPosition { get; set; }
        public override float StepSoundDistance { get; set; }
    }
}
