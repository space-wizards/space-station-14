#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part.Property
{
    /// <summary>
    ///     Defines the speed at which a <see cref="IBodyPart"/> can move.
    /// </summary>
    [RegisterComponent]
    public class LegComponent : BodyPartPropertyComponent
    {
        public override string Name => "Leg";

        /// <summary>
        ///     Speed in tiles per second.
        /// </summary>
        public float Speed { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, l => l.Speed, "speed", 2.6f);
        }
    }
}
