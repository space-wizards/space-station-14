using Robust.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Movement;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent
    {
       
    }
}
