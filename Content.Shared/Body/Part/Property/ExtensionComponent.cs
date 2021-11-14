using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Part.Property
{
    /// <summary>
    ///     Defines the length of a <see cref="SharedBodyPartComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class ExtensionComponent : BodyPartPropertyComponent
    {
        public override string Name => "Extension";

        /// <summary>
        ///     Current distance in tiles.
        /// </summary>
        [DataField("distance")]
        public float Distance { get; set; } = 3f;
    }
}
