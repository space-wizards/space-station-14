using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Part.Property
{
    /// <summary>
    ///     Defines the speed at which a <see cref="SharedBodyPartComponent"/> can move.
    /// </summary>
    [RegisterComponent]
    public class LegComponent : BodyPartPropertyComponent
    {
        public override string Name => "Leg";

        /// <summary>
        ///     Speed in tiles per second.
        /// </summary>
        [DataField("speed")]
        public float Speed { get; set; } = 2.6f;
    }
}
