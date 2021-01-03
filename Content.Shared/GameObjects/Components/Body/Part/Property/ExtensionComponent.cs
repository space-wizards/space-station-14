using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part.Property
{
    /// <summary>
    ///     Defines the length of a <see cref="IBodyPart"/>.
    /// </summary>
    [RegisterComponent]
    public class ExtensionComponent : BodyPartPropertyComponent
    {
        public override string Name => "Extension";

        /// <summary>
        ///     Current distance in tiles.
        /// </summary>
        [YamlField("distance")]
        public float Distance { get; set; } = 3f;
    }
}
