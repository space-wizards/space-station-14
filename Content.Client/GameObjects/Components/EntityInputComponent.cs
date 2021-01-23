using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    /// Needed because SharedEntityInputCompnents PreventCollision function needs to be called on the client as well.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedEntityInputComponent))]
    public class EntityInputComponent : SharedEntityInputComponent
    {
    }
}
