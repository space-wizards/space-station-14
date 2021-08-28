using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Shared.Verbs
{
    /// <summary>
    ///     Stores visual data for a verb.
    /// </summary>
    /// <remarks>
    ///     An instance of this class gets instantiated by the verb system and should be filled in by implementations of
    ///     <see cref="OldVerb.GetData(IEntity, IComponent, OldVerbData)"/>.
    /// </remarks>
    public sealed class OldVerbData
    {
        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        /// <remarks>
        ///     This string is automatically passed through Loc.GetString().
        /// </remarks>
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

    /// <summary>
    ///     Using delegate to store information about verb execution.
    /// </summary>
    public delegate void RaiseVerb();

    public class Verb
    {
        /// <summary>
        ///     Performs the verbs associated action.
        /// </summary>
        /// <remarks>
        ///     This is probably either some function in the system assembling this verb, or a lambda function that raises some event.
        /// </remarks>
        public RaiseVerb Execute;

        /// <summary>
        ///     The key that is used to refer to a specific verb for execution.
        /// </summary>
        public string Key;

        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        public string Text = string.Empty;

        /// <remarks>
        ///     Convenience property to run localization on verb text.
        /// </remarks>
        public string LocText { set => Text = Loc.GetString(value); }

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? Icon;

        /// <summary>
        ///     Name of the category this button is under. Used to group verbs in the context menu.
        /// </summary>
        public string Category = string.Empty;

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? CategoryIcon;

        /// <summary>
        ///     Whether this verb is visible, but, disabled (greyed out).
        /// </summary>
        public bool IsDisabled;

        /// <summary>
        ///     Convenience property to set <see cref="Icon"/> to a raw texture path.
        /// </summary>
        public string IconTexture
        {
            set => Icon = new SpriteSpecifier.Texture(new ResourcePath(value));
        }

        public Verb(string key,  RaiseVerb execute )
        {
            Execute = execute;
            Key = key;
        }
    }
}
