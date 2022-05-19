namespace Content.Server.Singularity.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to a singularity.
    /// </summary>
    [RegisterComponent]
    public sealed class SinguloFoodComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        public int Energy { get; set; } = 1;
    }
}
