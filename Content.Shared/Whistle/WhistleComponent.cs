namespace Content.Shared.Whistle
{
    /// <summary>
    /// Spawn attached entity for entities in range with MobMoverComponent.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WhistleComponent : Component
    {
        /// <summary>
        /// Entity prototype to spawn
        /// </summary>
        [DataField("effect")]
        public string? effect = "WhistleExclamation"; 

        /// <summary>
        /// Range value.
        /// </summary>
        [DataField("distance")]
        public float Distance = 0;
    }
}
