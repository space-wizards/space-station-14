#nullable enable
using Content.Shared.Rotatable;
using Robust.Shared.GameObjects;

namespace Content.Client.Rotatable
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRotatableComponent))]
    public class RotatableComponent : SharedRotatableComponent
    {
    }
}
