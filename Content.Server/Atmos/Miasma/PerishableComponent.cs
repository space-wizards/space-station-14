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
        public TimeSpan RotAfter = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Gasses are released every second.
        /// </summary>
        [DataField("rotAccumulator")]
        public float RotAccumulator = 0f;

        /// <summary>
        /// How many moles of gas released per second, adjusted for mass.
        /// Humans have a mass of 70. I am aiming for ten mols a minute, so
        /// 1/6 of a minute, divided by 70 as a baseline.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MolsPerSecondPerUnitMass = 0.0025f;
    }
}
