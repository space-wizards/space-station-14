using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part.Property.Movement
{
    public abstract class LegProperty : BodyPartPropertyComponent
    {
        public override string Name => "Leg";

        /// <summary>
        ///     Speed (in tiles per second).
        /// </summary>
        public float Speed { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, l => l.Speed, "speed", 1f);
        }
    }
}
