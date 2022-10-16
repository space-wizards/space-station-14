using Content.Shared.MachineLinking;
using Content.Shared.TextScreen;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Client.TextScreen
{
    [RegisterComponent]
    public sealed class TextScreenVisualsComponent : Component
    {
        public const float PixelSize = 0.03125f;

        [DataField("color")]
        public Color Color { get; set; } = Color.White;

        [DataField("activated")]
        public bool Activated = false;

        [DataField("currentMode")]
        public TextScreenMode CurrentMode = TextScreenMode.Text;

        [DataField("targetTime")]
        public TimeSpan TargetTime = TimeSpan.Zero;

        [DataField("textOffset"), ViewVariables(VVAccess.ReadWrite)]
        public Vector2 TextOffset = new(0f, PixelSize*8);

        /// <summary>
        ///     The amount of characters this component can show.
        /// </summary>
        [DataField("textLength")]
        public int TextLength = 5;

        /// <summary>
        ///     The text to update from
        /// </summary>
        public string ShowText = "";

        /// <summary>
        ///     The text to show
        /// </summary>
        [DataField("text"), ViewVariables(VVAccess.ReadWrite)]
        public string Text = "";

        /// <summary>
        ///     The different layers for each character.
        /// </summary>
        [DataField("textLayers")]
        public Dictionary<string, string?> TextLayers = new();
    }
}
