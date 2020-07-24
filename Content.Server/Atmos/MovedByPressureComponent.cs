using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        public override string Name => "MovedByPressure";

        public float PressureResistance { get; set; } = 1f;
        public float MoveResist { get; set; } = 100f;
        public int LastHighPressureMovementAirCycle { get; set; } = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => PressureResistance, "pressureResistance", 1f);
            serializer.DataField(this, x => MoveResist, "moveResist", 100f);
        }
    }
}
