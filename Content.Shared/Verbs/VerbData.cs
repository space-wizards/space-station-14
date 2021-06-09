#nullable enable
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.Verbs
{
    /// <summary>
    ///     Stores visual data for a verb.
    /// </summary>
    /// <remarks>
    ///     An instance of this class gets instantiated by the verb system and should be filled in by implementations of
    ///     <see cref="Verb.GetData(IEntity, IComponent, VerbData)"/>.
    /// </remarks>
    public sealed class VerbData
    {
        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? Icon { get; set; }

        /// <summary>
        ///     Name of the category this button is under.
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? CategoryIcon { get; set; }

        /// <summary>
        ///     Whether this verb is visible, disabled (greyed out) or hidden.
        /// </summary>
        public VerbVisibility Visibility { get; set; } = VerbVisibility.Visible;

        public bool IsInvisible => Visibility == VerbVisibility.Invisible;
        public bool IsDisabled => Visibility == VerbVisibility.Disabled;

        /// <summary>
        ///     Convenience property to set verb category and icon at once.
        /// </summary>
        [ValueProvider("Content.Shared.GameObjects.VerbCategories")]
        public VerbCategoryData CategoryData
        {
            set
            {
                Category = value.Name;
                CategoryIcon = value.Icon;
            }
        }

        /// <summary>
        ///     Convenience property to set <see cref="Icon"/> to a raw texture path.
        /// </summary>
        public string IconTexture
        {
            set => Icon = new SpriteSpecifier.Texture(new ResourcePath(value));
        }
    }
}
