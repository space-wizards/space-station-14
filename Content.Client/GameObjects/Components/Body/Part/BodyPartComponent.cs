using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    [ComponentReference(typeof(IBodyPart))]
    public class BodyPartComponent : SharedBodyPartComponent
    {
    }
}
