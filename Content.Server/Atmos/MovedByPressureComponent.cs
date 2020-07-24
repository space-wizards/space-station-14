using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        public override string Name => "MovedByPressure";

        public float PressureResistance { get; set; } = 10f;
        public float MoveResist { get; set; } = 10f;
        public int LastHighPressureMovementAirCycle { get; set; } = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => PressureResistance, "pressureResistance", 10f);
            serializer.DataField(this, x => MoveResist, "moveResist", 1000f);
        }
    }
}
