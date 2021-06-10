#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Part.Property
{
    /// <summary>
    ///     Defines a property for a <see cref="IBodyPart"/>.
    /// </summary>
    public interface IBodyPartProperty : IComponent
    {
        bool Active { get; set; }
    }
}
