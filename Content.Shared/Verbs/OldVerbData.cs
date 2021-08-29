using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;

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

    [Serializable, NetSerializable]
    public class Verb : IComparable
    {
        /// <summary>
        ///     This action "acts" out the verb.
        /// </summary>
        /// <remarks>
        ///     This is probably either some function in the system assembling this verb, or a lambda function that raises some event.
        /// </remarks>
        [NonSerialized]
        public Action? Act;

        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        public string Text = string.Empty;

        /// <summary>
        ///     The key used to identify the verb when communicating over the network.
        /// </summary>
        public string Key;

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

        /// <remarks>
        ///     Convenience property to run localization on verb category.
        /// </remarks>
        public string LocCategory { set => Category = Loc.GetString(value); }

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? CategoryIcon;

        /// <summary>
        ///     Whether this verb is visible, but, disabled (greyed out).
        /// </summary>
        public bool IsDisabled;

        /// <summary>
        ///     Determines the priority of the verb. This affects both how the verb is displayed in the context menu
        ///     GUI, and which verb is actually executed when left/alt clicking.
        /// </summary>
        /// <remarks>
        ///     Bigger is higher priority (appears first, gets executed preferentially).
        /// </remarks>
        public int Priority;

        /// <summary>
        ///     Convenience property to set <see cref="Icon"/> to a raw texture path.
        /// </summary>
        public string IconTexture
        {
            set => Icon = new SpriteSpecifier.Texture(new ResourcePath(value));
        }

        public Verb(string key)
        {
            Key = key;
        }

        /// <summary>
        ///     Allow verbs to be compared to each other for sorting. Sorting is based on the Priority variable, with alphabetical sorting as fall-back.
        /// </summary>
        public int CompareTo(object? obj)
        {
            if (obj is not Verb otherVerb)
                return -1;

            // Sort first by priority
            if (Priority != otherVerb.Priority)
                return otherVerb.Priority - Priority;

            // Then try use alphabetical verb categories. This puts uncategorized verbs first.
            // TODO VERBS maybe make categories appear first, which is the pre-ECS behavior?
            if (Category != otherVerb.Category)
                return string.Compare(Category, otherVerb.Category, StringComparison.CurrentCulture);

            // Finally, use verb text as tie-breaker
            return string.Compare(Text,otherVerb.Text, StringComparison.CurrentCulture);
        }
    }
}
