namespace Content.Server.Whistle.Components
{
    [RegisterComponent]
    public sealed partial class WhistleComponent : Component
    {
        [DataField("effect")]
        public string? effect = "WhistleExclamation";

        [DataField("distance")]
        public float Distance = 0;
    }
}
