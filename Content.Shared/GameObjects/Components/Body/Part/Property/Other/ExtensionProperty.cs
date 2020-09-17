using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part.Property.Other
{
    [RegisterComponent]
    public class ExtensionProperty : BodyPartPropertyComponent
    {
        public override string Name => "Extension";

        /// <summary>
        ///     Current reach distance (in tiles).
        /// </summary>
        public float ReachDistance { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, e => e.ReachDistance, "reachDistance", 2f);
        }
    }
}
