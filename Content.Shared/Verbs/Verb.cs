using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;

namespace Content.Shared.Verbs
{
    [Flags]
    public enum VerbType
    {
        Interaction = 1,
        Activation = 2,
        Alternative = 4,
        Other = 8,
        All = 1+2+4+8
    }

    /// <summary>
    ///     Verb objects describe actions that a user can take. The actions can be specified via an Action, local
    ///     events, or networked events. Verbs also provide text, icons, and categories for displaying in the
    ///     context-menu.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class Verb : IComparable
    {
        /// <summary>
        ///     This is an action that will be run when the verb is "acted" out.
        /// </summary>
        /// <remarks>
        ///     This delegate probably just points to some function in the system assembling this verb. This delegate
        ///     will be run regardless of whether <see cref="LocalVerbEventArgs"/> or <see cref="NetworkVerbEventArgs"/>
        ///     are defined.
        /// </remarks>
        [NonSerialized]
        public Action? Act;

        /// <summary>
        ///     This is local event that will be raised when the verb is executed.
        /// </summary>
        /// <remarks>
        ///     This event will be raised regardless of whether <see cref="NetworkVerbEventArgs"/> or <see cref="Act"/>
        ///     are defined.
        /// </remarks>
        [NonSerialized]
        public object? LocalVerbEventArgs;

        /// <summary>
        ///     Where do direct the local event.
        /// </summary>
        [NonSerialized]
        public EntityUid LocalEventTarget = EntityUid.Invalid;

        /// <summary>
        ///     This is networked event that will be raised when the verb is executed.
        /// </summary>
        /// <remarks>
        ///     This event will be raised regardless of whether <see cref="LocalVerbEventArgs"/> or <see cref="Act"/>
        ///     are defined.
        /// </remarks>
        [NonSerialized]
        public EntityEventArgs? NetworkVerbEventArgs;
        
        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        public string Text = string.Empty;

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? Icon
        {
            get => _icon ??=
                IconTexture == null ? null : new SpriteSpecifier.Texture(new ResourcePath(IconTexture));
            set => _icon = value;
        }
        private SpriteSpecifier? _icon;

        /// <summary>
        ///     Name of the category this button is under. Used to group verbs in the context menu.
        /// </summary>
        public VerbCategory? Category;

        /// <summary>
        ///     Whether this verb is disabled.
        /// </summary>
        /// <remarks>
        ///     Disabled verbs are shown in the context menu with a slightly darker background color, and cannot be
        ///     executed. It is recommended that a <see cref="Tooltip"/> message be provided outlining why this verb is
        ///     disabled.
        /// </remarks>
        public bool Disabled;

        /// <summary>
        ///     Optional tooltip to show when hovering over this verb. 
        /// </summary>
        /// <remarks>
        ///     Useful for disabled verbs as a replacement for informative pop-up messages.
        /// </remarks>
        public string? Tooltip;

        /// <summary>
        ///     Determines the priority of the verb. This affects both how the verb is displayed in the context menu
        ///     GUI, and which verb is actually executed when left/alt clicking.
        /// </summary>
        /// <remarks>
        ///     Bigger is higher priority (appears first, gets executed preferentially).
        /// </remarks>
        public int Priority;

        /// <summary>
        ///     Raw texture path used to load the <see cref="Icon"/>.
        /// </summary>
        public string? IconTexture;

        /// <summary>
        ///     Whether or not to close the context menu after using it to run this verb.
        /// </summary>
        /// <remarks>
        ///     Setting this to false may be useful for repeatable actions, like rotating an object or maybe knocking on
        ///     a window.
        /// </remarks>
        public bool CloseMenu = true;

        /// <summary>
        ///     Compares two verbs based on their <see cref="Priority"/>, <see cref="Category"/>, <see cref="Text"/>,
        ///     and <see cref="IconTexture"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This is comparison is used when storing verbs in a SortedSet. The ordering of verbs determines both how
        ///     the verbs are displayed in the context menu, and the order in which alternative action verbs are
        ///     executed when alt-clicking.
        ///     </para>
        ///     <para>
        ///     If two verbs are equal according to this comparison, they cannot both be added to the same sorted set of
        ///     verbs. This is desirable, given that these verbs would also appear identical in the context menu.
        ///     Distinct verbs should always have a unique and descriptive combination of text, icon, and category.
        ///     </para>
        /// </remarks>
        public int CompareTo(object? obj)
        {
            if (obj is not Verb otherVerb)
                return -1;

            // Sort first by priority
            if (Priority != otherVerb.Priority)
                return otherVerb.Priority - Priority;

            // Then try use alphabetical verb categories. Uncategorized verbs always appear first.
            if (Category?.Text != otherVerb.Category?.Text)
            {
                return string.Compare(Category?.Text, otherVerb.Category?.Text, StringComparison.CurrentCulture);
            }
            
            // Then try use alphabetical verb text.
            if (Text != otherVerb.Text)
            {
                return string.Compare(Text, otherVerb.Text, StringComparison.CurrentCulture);
            }

            // Finally, compare icon texture paths. Note that this matters for verbs that don't have any text (e.g., the rotate-verbs)
            return string.Compare(IconTexture, otherVerb.IconTexture, StringComparison.CurrentCulture);
        }
    }
}
