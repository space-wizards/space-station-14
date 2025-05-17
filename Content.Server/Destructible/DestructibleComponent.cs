using Content.Server.Destructible.Thresholds;

namespace Content.Server.Destructible
{
    /// <summary>
    ///     When attached to an <see cref="Robust.Shared.GameObjects.EntityUid"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DestructibleComponent : Component
    {
        /// <summary>
        /// A list of damage thresholds for the entity;
        /// includes their triggers and resultant behaviors
        /// </summary>
        [DataField]
        public List<DamageThreshold> Thresholds = new();

        /// <summary>
        /// Specifies whether the entity has passed a damage threshold that causes it to break
        /// </summary>
        [DataField]
        public bool IsBroken = false;
    }
}
