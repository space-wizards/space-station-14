namespace Content.Server.Baseball
{

    /// <summary>
    /// This is used for...
    /// </summary>
    [RegisterComponent]
    public sealed class BaseballBatComponent : Component
    {
        [DataField("wackForceMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WackForceMultiplier {get; set; } = 5f;
    }
}
