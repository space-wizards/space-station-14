namespace Content.Shared.Examine
{
    /// <summary>
    /// Entities with this component tries to have their detailed stats condensed to a single tooltip.
    /// </summary>
    [RegisterComponent]
    public sealed class CondensedExamineComponent : Component
    {
        [DataField("icon")]
        public string Icon = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";

        [DataField("text")]
        public string Text = "condensed-examine-default-text";

        [DataField("message")]
        public string Message = "condensed-examine-default-message";

        [DataField("firstline")]
        public string FirstLine = "condensed-examine-default-first-line";

        [DataField("access")]
        public bool CanAccess = true;

        [DataField("interact")]
        public bool CanInteract = true;
    }
}
