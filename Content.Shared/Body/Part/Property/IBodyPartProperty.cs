using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Part.Property
{
    /// <summary>
    ///     Defines a property for a <see cref="SharedBodyPartComponent"/>.
    /// </summary>
    public interface IBodyPartProperty : IComponent
    {
        bool Active { get; set; }
    }
}
