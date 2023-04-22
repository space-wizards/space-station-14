namespace Content.Client.Smoking;

    [RegisterComponent]
    public sealed class BurnStateVisualsComponent : Component
    {
        [DataField("burntIcon")]
        public string _burntIcon = "burnt-icon";
        [DataField("litIcon")]
        public string _litIcon = "lit-icon";
        [DataField("unlitIcon")]
        public string _unlitIcon = "icon";
    }

