#nullable enable
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    public interface IHasBody : IComponent
    {
        /// <summary>
        ///     The body that this component is currently a part of, if any.
        /// </summary>
        IBody? Body { get; }
    }
}
