#nullable enable
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        public override string Name => "MovedByPressure";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)]
        public float PressureResistance { get; set; } = 1f;
        [ViewVariables(VVAccess.ReadWrite)]
        public float MoveResist { get; set; } = 100f;
        [ViewVariables(VVAccess.ReadWrite)]
        public int LastHighPressureMovementAirCycle { get; set; } = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Enabled, "enabled", true);
            serializer.DataField(this, x => PressureResistance, "pressureResistance", 1f);
            serializer.DataField(this, x => MoveResist, "moveResist", 100f);
        }
    }

    public static class MovedByPressureExtensions
    {
        public static bool IsMovedByPressure(this IEntity entity)
        {
            return entity.IsMovedByPressure(out _);
        }

        public static bool IsMovedByPressure(this IEntity entity, [NotNullWhen(true)] out MovedByPressureComponent? moved)
        {
            return entity.TryGetComponent(out moved) &&
                   moved.Enabled;
        }
    }
}
