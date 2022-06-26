namespace Content.Client.Kudzu
{
    [RegisterComponent]
    public sealed class KudzuVisualsComponent : Component
    {
        [DataField("layer")]
        public int Layer { get; } = 0;
    }

}
