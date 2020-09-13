#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public interface IMechanismBehavior : IHasBody
    {
        IBodyPart? Part { get; }

        /// <summary>
        ///     Upward reference to the parent <see cref="IMechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        IMechanism? Mechanism { get; }

        void Update(float frameTime);
    }
}
