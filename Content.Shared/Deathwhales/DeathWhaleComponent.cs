namespace Content.Server.Deathwhale
{
    [RegisterComponent]
    public sealed partial class DeathWhaleComponent : Component
    {

         [DataField("radius")]
         public float Radius = 6;

        [DataField("caughtPrey")]
        public bool caughtPrey = false;

    }
}
