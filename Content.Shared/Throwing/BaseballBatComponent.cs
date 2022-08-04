namespace Content.Shared.Throwing
{

    /// <summary>
    /// This is used for...
    /// </summary>
    [RegisterComponent]
    public sealed class BaseballBatComponent : Component
    {
        [DataField("wackForceMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WackForceMultiplier {get; set; } = 10f;
    }
}
