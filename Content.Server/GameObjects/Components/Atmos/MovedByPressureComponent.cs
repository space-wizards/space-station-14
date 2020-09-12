using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        public override string Name => "MovedByPressure";

        [ViewVariables(VVAccess.ReadWrite)]
        public float PressureResistance { get; set; } = 1f;
        [ViewVariables(VVAccess.ReadWrite)]
        public float MoveResist { get; set; } = 100f;
        [ViewVariables]
        public int LastHighPressureMovementAirCycle { get; set; } = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => PressureResistance, "pressureResistance", 1f);
            serializer.DataField(this, x => MoveResist, "moveResist", 100f);
        }
    }
}
