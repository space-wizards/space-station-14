namespace Content.Client.Kudzu
{
    [RegisterComponent]
    public sealed class KudzuVisualizerComponent : Component
    {
        [DataField("layer")]
        public int Layer { get; } = 0;
    }

}
