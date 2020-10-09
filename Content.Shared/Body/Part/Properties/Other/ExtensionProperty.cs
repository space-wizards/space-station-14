using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part.Properties.Other
{
    /// <summary>
    ///     Defines the extension ability of a BodyPart. Used to determine things like reach distance and running speed.
    /// </summary>
    public class ExtensionProperty : BodyPartProperty
    {
        /// <summary>
        ///     Current reach distance (in tiles).
        /// </summary>
        public float ReachDistance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref ReachDistance, "reachDistance", 2f);
        }
    }
}
