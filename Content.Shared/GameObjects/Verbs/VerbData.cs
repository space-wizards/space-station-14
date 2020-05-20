using JetBrains.Annotations;

namespace Content.Shared.GameObjects
{
    public sealed class VerbData
    {
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; } = "";
        public string CategoryIcon { get; set; }
        public VerbVisibility Visibility { get; set; } = VerbVisibility.Visible;

        public bool IsInvisible => Visibility == VerbVisibility.Invisible;
        public bool IsDisabled => Visibility == VerbVisibility.Disabled;

        [ValueProvider("Content.Shared.GameObjects.VerbCategories")]
        public VerbCategoryData CategoryData
        {
            set
            {
                Category = value.Name;
                CategoryIcon = value.Icon;
            }
        }
    }
}
