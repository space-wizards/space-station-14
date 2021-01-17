using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Part.Property
{
    /// <summary>
    ///     Defines a <see cref="IBodyPart"/> as being able to grasp around an entity,
    ///     for example picking up an item.
    /// </summary>
    // TODO BODY Implement
    [RegisterComponent]
    public class GraspComponent : BodyPartPropertyComponent
    {
        public override string Name => "Grasp";
    }
}
