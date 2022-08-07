namespace Content.Server.Sports.Components
{

    /// <summary>
    /// Give this to a melee weapon with wide attack and it will be able to bat thrown objects
    /// </summary>
    [RegisterComponent]
    public sealed class BaseballBatComponent : Component
    {
        [DataField("wackForceMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WackForceMultiplier {get; set; } = 5f;
    }
}
