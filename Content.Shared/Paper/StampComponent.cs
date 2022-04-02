namespace Content.Shared.Paper
{
    [RegisterComponent]
    public class StampComponent : Component
    {
        /// <summary>
        ///     The name that will be stamped to the piece of paper on examine.
        /// </summary>
        [ViewVariables]
        [DataField("stampedName")]
        public string StampedName { get; set; } = "A very important person";
    }
}
