using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

#nullable enable

namespace Content.Server.GameObjects.Components.Movement
{
    /// <summary>
    ///     Moves the entity based on input from a KeyBindingInputComponent.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class PlayerInputMoverComponent : SharedPlayerInputMoverComponent, IMoverComponent, ICollideSpecial
    {
        public override GridCoordinates LastPosition { get; set; }

        public override float StepSoundDistance { get; set; }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            // Don't collide with other mobs
            if (collidedwith.Owner.TryGetComponent<SpeciesComponent>(out var collidedSpeciesComponent))
            {
                return true;
            }
            return false;
        }
    }
}
