namespace Content.Server.Electrocution
{
    [RegisterComponent]
    public sealed partial class RandomInsulationComponent : Component
    {
        [DataField("list")]
        public readonly float[] List = { 0f };
    }
}
