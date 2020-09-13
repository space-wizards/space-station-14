using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent
    {
       
    }
}
