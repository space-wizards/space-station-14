#nullable enable
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class MovedByPressureComponent : Component
    {
        public override string Name => "MovedByPressure";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressureResistance")]
        public float PressureResistance { get; set; } = 1f;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("moveResist")]
        public float MoveResist { get; set; } = 100f;
        [ViewVariables(VVAccess.ReadWrite)]
        public int LastHighPressureMovementAirCycle { get; set; } = 0;
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
