namespace Content.Client.Smoking;

    [RegisterComponent]
    public sealed class BurnStateVisualsComponent : Component
    {
        [DataField("burntIcon")]
        public string burntIcon = "burnt-icon";
        [DataField("litIcon")]
        public string litIcon = "lit-icon";
        [DataField("unlitIcon")]
        public string unlitIcon = "icon";
    }

