using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Atmos.Miasma
{
    /// <summary>
    /// This makes mobs eventually start rotting when they die.
    /// It may be expanded to food at some point, but it's just for mobs right now.
    /// </summary>
    [RegisterComponent]
    public sealed class PerishableComponent : Component
    {
        /// <summary>
        /// Is this progressing?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Progressing = true;

        /// <summary>
        /// How long this creature has been dead.
        /// </summary>
        [DataField("timeOfDeath", customTypeSerializer: typeof(TimeOffsetSerializer))]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan TimeOfDeath = TimeSpan.Zero;

        /// <summary>
        /// When DeathAccumulator is greater than this, start rotting.
        /// </summary>
        public TimeSpan RotAfter = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gasses are released, this is when the next gas release update will be.
        /// </summary>
        [DataField("rotNextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan RotNextUpdate = TimeSpan.Zero;

        /// <summary>
        /// How many moles of gas released per second, per unit of mass.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("molsPerSecondPerUnitMass")]
        public float MolsPerSecondPerUnitMass = 0.0025f;
    }
}
