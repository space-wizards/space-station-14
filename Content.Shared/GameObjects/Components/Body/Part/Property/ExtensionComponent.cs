using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part.Property
{
    [RegisterComponent]
    public class ExtensionComponent : BodyPartPropertyComponent
    {
        public override string Name => "Extension";

        /// <summary>
        ///     Current distance (in tiles).
        /// </summary>
        public float Distance { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, e => e.Distance, "distance", 3f);
        }
    }
}
