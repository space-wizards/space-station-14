namespace Content.Client.Kudzu
{
    [RegisterComponent]
    public sealed partial class KudzuVisualsComponent : Component
    {
        [DataField("layer")]
        public int Layer { get; private set; } = 0;
    }

}
