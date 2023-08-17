namespace Content.Server.Electrocution
{
    [RegisterComponent]
    public sealed partial class RandomInsulationComponent : Component
    {
        [DataField("list")]
        public float[] List { get; private set; } = { 0f };
    }
}
