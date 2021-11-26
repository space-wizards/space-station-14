using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Part.Property
{
    /// <summary>
    ///     Property attachable to a <see cref="SharedBodyPartComponent"/>.
    ///     For example, this is used to define the speed capabilities of a leg.
    ///     The movement system will look for a <see cref="LegComponent"/> on all
    ///     <see cref="SharedBodyPartComponent"/>.
    /// </summary>
    public abstract class BodyPartPropertyComponent : Component, IBodyPartProperty
    {
        /// <summary>
        ///     Whether this property is currently active.
        /// </summary>
        [DataField("active")]
        public bool Active { get; set; } = true;
    }
}
