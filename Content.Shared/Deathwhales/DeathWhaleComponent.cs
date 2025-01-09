namespace Content.Server.Deathwhale
{
    [RegisterComponent]
    public sealed partial class DeathWhaleComponent : Component
    {

         [DataField("radius")]
         public float Radius = 50;

        [DataField("caughtPrey")]
        public var caughtPrey = null;
        
    }
}
