using Robust.Shared.Serialization;

namespace Content.Shared.Body.BodyPart.BodyPartProperties.Movement
{
    /// <summary>
    ///     Defines the speed of humanoid-like movement. Must be connected to a <see cref="BodyPart" /> with
    ///     <see cref="FootProperty" /> and have
    ///     <see cref="ExtensionProperty" /> on the same organ and down to the foot to work.
    /// </summary>
    public class LegProperty : BodyPartProperty
    {
        /// <summary>
        ///     Speed (in tiles per second).
        /// </summary>
        public float Speed;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Speed, "speed", 1f);
        }
    }
}
