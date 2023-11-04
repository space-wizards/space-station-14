namespace Content.Server.Electrocution
{
    [RegisterComponent]
    public sealed partial class RandomInsulationComponent : Component
    {
        [DataField("list")]
        public float[] List = { 0f };
    }
}
