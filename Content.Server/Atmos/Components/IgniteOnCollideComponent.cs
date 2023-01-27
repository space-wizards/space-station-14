namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class IgniteOnCollideComponent : Component
    {
        [DataField("fireStacks")]
        public float FireStacks { get; set; }
    }
}
