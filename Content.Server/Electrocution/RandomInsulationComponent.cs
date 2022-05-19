namespace Content.Server.Electrocution
{
    [RegisterComponent]
    public sealed class RandomInsulationComponent : Component
    {
        [DataField("list")]
        public readonly float[] List = { 0f };
    }
}
