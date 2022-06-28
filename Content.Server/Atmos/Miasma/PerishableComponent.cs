namespace Content.Server.Atmos.Miasma
{
    [RegisterComponent]
    /// <summary>
    /// This makes mobs eventually start rotting when they die.
    /// It may be expanded to food at some point, but it's just for mobs right now.
    /// </summary>
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
        [DataField("deathAccumulator")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float DeathAccumulator = 0f;

        /// <summary>
        /// When DeathAccumulator is greater than this, start rotting.
        /// </summary>
        public TimeSpan RotAfter = TimeSpan.FromMinutes(5);

        public bool Rotting => (DeathAccumulator > RotAfter.TotalSeconds);

        /// <summary>
        /// Gasses are released every second.
        /// </summary>
        [DataField("rotAccumulator")]
        public float RotAccumulator = 0f;

        /// <summary>
        /// How many moles of gas released per second, per unit of mass.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("molsPerSecondPerUnitMass")]
        public float MolsPerSecondPerUnitMass = 0.0025f;
    }
}
