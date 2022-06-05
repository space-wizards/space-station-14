namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class IgniteOnCollideComponent : Component
    {
        [DataField("fireStacks")]
        public float FireStacks { get; set; }
    }
}
