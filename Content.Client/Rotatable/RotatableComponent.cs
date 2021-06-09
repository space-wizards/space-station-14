#nullable enable
using Content.Shared.GameObjects.Components.Rotatable;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Rotatable
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRotatableComponent))]
    public class RotatableComponent : SharedRotatableComponent
    {
    }
}
