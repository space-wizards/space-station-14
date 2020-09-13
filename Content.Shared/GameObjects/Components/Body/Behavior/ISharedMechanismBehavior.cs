#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public interface ISharedMechanismBehavior : IHasBody
    {
        ISharedBodyPart? Part { get; }

        /// <summary>
        ///     Upward reference to the parent <see cref="ISharedMechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        ISharedMechanism? Mechanism { get; }

        void Update(float frameTime);
    }
}
