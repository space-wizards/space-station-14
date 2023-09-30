using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Verbs
{
    /// <summary>
    ///     Verb objects describe actions that a user can take. The actions can be specified via an Action, local
    ///     events, or networked events. Verbs also provide text, icons, and categories for displaying in the
    ///     context-menu.
    /// </summary>
    [Serializable, NetSerializable, Virtual]
    public class Verb : IComparable
    {
        public static string DefaultTextStyleClass = "Verb";

        /// <summary>
        ///     Determines the priority of this type of verb when displaying in the verb-menu. See <see
        ///     cref="CompareTo"/>.
        /// </summary>
        public virtual int TypePriority => 0;

        /// <summary>
        ///     Style class for drawing in the context menu
        /// </summary>
        public string TextStyleClass = DefaultTextStyleClass;

        /// <summary>
        ///     This is an action that will be run when the verb is "acted" out.
        /// </summary>
        /// <remarks>
        ///     This delegate probably just points to some function in the system assembling this verb. This delegate
        ///     will be run regardless of whether <see cref="ExecutionEventArgs"/> is defined.
        /// </remarks>
        [NonSerialized]
        public Action? Act;

        /// <summary>
        ///     This is a general local event that will be raised when the verb is executed.
        /// </summary>
        /// <remarks>
        ///     If not null, this event will be raised regardless of whether <see cref="Act"/> was run. If this event
        ///     exists purely to call a specific system method, then <see cref="Act"/> should probably be used instead (method
        ///     events are a no-go).
        /// </remarks>
        [NonSerialized]
        public object? ExecutionEventArgs;

        /// <summary>
        ///     Where do direct the local event. If invalid, the event is not raised directed at any entity.
        /// </summary>
        [NonSerialized]
        public EntityUid EventTarget = EntityUid.Invalid;

        /// <summary>
        ///     Whether a verb is only defined client-side. Note that this has nothing to do with whether the target of
        ///     the verb is client-side
        /// </summary>
        /// <remarks>
        ///     If true, the client will not also ask the server to run this verb when executed locally. This just
        ///     prevents unnecessary network events and "404-verb-not-found" log entries.
        /// </remarks>
        [NonSerialized]
        public bool ClientExclusive;

        /// <summary>
        ///     The text that the user sees on the verb button.
        /// </summary>
        public string Text = string.Empty;

        /// <summary>
        ///     Sprite of the icon that the user sees on the verb button.
        /// </summary>
        public SpriteSpecifier? Icon;

        /// <summary>
        ///     Name of the category this button is under. Used to group verbs in the context menu.
        /// </summary>
        public VerbCategory? Category;

        /// <summary>
        ///     Whether this verb is disabled.
        /// </summary>
        /// <remarks>
        ///     Disabled verbs are shown in the context menu with a slightly darker background color, and cannot be
        ///     executed. It is recommended that a <see cref="Message"/> message be provided outlining why this verb is
        ///     disabled.
        /// </remarks>
        public bool Disabled;

        /// <summary>
        ///     Optional informative message.
        /// </summary>
        /// <remarks>
        ///     This will be shown as a tooltip when hovering over this verb in the context menu. Additionally, iF a
        ///     <see cref="Disabled"/> verb is executed, this message will also be shown as a pop-up message. Useful for
        ///     disabled verbs to inform users about why they cannot perform a given action.
        /// </remarks>
        public string? Message;

        /// <summary>
        ///     Determines the priority of the verb. This affects both how the verb is displayed in the context menu
        ///     GUI, and which verb is actually executed when left/alt clicking.
        /// </summary>
        /// <remarks>
        ///     Bigger is higher priority (appears first, gets executed preferentially).
        /// </remarks>
        public int Priority;

        /// <summary>
        ///     If this is not null, and no icon or icon texture were specified, a sprite view of this entity will be
        ///     used as the icon for this verb.
        /// </summary>
        public NetEntity? IconEntity;

        /// <summary>
        ///     Whether or not to close the context menu after using it to run this verb.
        /// </summary>
        /// <remarks>
        ///     Setting this to false may be useful for repeatable actions, like rotating an object or maybe knocking on
        ///     a window.
        /// </remarks>
        public bool? CloseMenu;

        public virtual bool CloseMenuDefault => true;

        /// <summary>
        ///     How important is this verb, for the purposes of admin logging?
        /// </summary>
        /// <remarks>
        ///     If this is just opening a UI or ejecting an id card, this should probably be low.
        /// </remarks>
        public LogImpact Impact = LogImpact.Low;

        /// <summary>
        ///     Whether this verb requires confirmation before being executed.
        /// </summary>
        public bool ConfirmationPopup = false;

        /// <summary>
        ///     If true, this verb will raise <see cref="ContactInteractionEvent"/>s when executed. If not explicitly
        ///     specified, this will just default to raising the event if <see cref="DefaultDoContactInteraction"/> is
        ///     true and the user is in range.
        /// </summary>
        public bool? DoContactInteraction;

        public virtual bool DefaultDoContactInteraction => false;

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

            // Sort first by type-priority
            if (TypePriority != otherVerb.TypePriority)
                return otherVerb.TypePriority - TypePriority;

            // Then by verb-priority
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

            if (IconEntity != otherVerb.IconEntity)
            {
                if (IconEntity == null)
                    return -1;

                if (otherVerb.IconEntity == null)
                    return 1;

                return IconEntity.Value.CompareTo(otherVerb.IconEntity.Value);
            }

            // Finally, compare icon texture paths. Note that this matters for verbs that don't have any text (e.g., the rotate-verbs)
            return string.Compare(Icon?.ToString(), otherVerb.Icon?.ToString(), StringComparison.CurrentCulture);
        }

        // I hate this. Please somebody allow generics to be networked.
        /// <summary>
        ///     Collection of all verb types,
        /// </summary>
        /// <remarks>
        ///     Useful when iterating over verb types, though maybe this should be obtained and stored via reflection or
        ///     something (list of all classes that inherit from Verb). Currently used for networking (apparently Type
        ///     is not serializable?), and resolving console commands.
        /// </remarks>
        public static List<Type> VerbTypes = new()
        {
            typeof(Verb),
            typeof(InteractionVerb),
            typeof(UtilityVerb),
            typeof(InnateVerb),
            typeof(AlternativeVerb),
            typeof(ActivationVerb),
            typeof(ExamineVerb),
            typeof(EquipmentVerb)
        };
    }

    /// <summary>
    ///    Primary interaction verbs. This includes both use-in-hand and interacting with external entities.
    /// </summary>
    /// <remarks>
    ///    These verbs those that involve using the hands or the currently held item on some entity. These verbs usually
    ///    correspond to interactions that can be triggered by left-clicking or using 'Z', and often depend on the
    ///    currently held item. These verbs are collectively shown first in the context menu.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class InteractionVerb : Verb
    {
        public new static string DefaultTextStyleClass = "InteractionVerb";
        public override int TypePriority => 4;
        public override bool DefaultDoContactInteraction => true;

        public InteractionVerb() : base()
        {
            TextStyleClass = DefaultTextStyleClass;
        }
    }

    /// <summary>
    ///     These verbs are similar to the normal interaction verbs, except these interactions are facilitated by the
    ///     currently held entity.
    /// </summary>
    /// <remarks>
    ///     The only notable difference between these and InteractionVerbs is that they are obtained by raising an event
    ///     directed at the currently held entity. Distinguishing between utility and interaction verbs helps avoid
    ///     confusion if a component enables verbs both when the item is used on something else, or when it is the
    ///     target of an interaction. These verbs are only obtained if the target and the held entity are NOT the same.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class UtilityVerb : Verb
    {
        public override int TypePriority => 3;
        public override bool DefaultDoContactInteraction => true;

        public UtilityVerb() : base()
        {
            TextStyleClass = InteractionVerb.DefaultTextStyleClass;
        }
    }

    /// <summary>
    ///     This is for verbs facilitated by components on the user.
    ///     Verbs from clothing, species, etc. rather than a held item.
    /// </summary>
    /// <remarks>
    ///     Add a component to the user's entity and sub to the get verbs event
    ///     and it'll appear in the verbs menu on any target.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class InnateVerb : Verb
    {
        public override int TypePriority => 3;
        public InnateVerb() : base()
        {
            TextStyleClass = InteractionVerb.DefaultTextStyleClass;
        }
    }

    /// <summary>
    ///     Verbs for alternative-interactions.
    /// </summary>
    /// <remarks>
    ///     When interacting with an entity via alt + left-click/E/Z the highest priority alt-interact verb is executed.
    ///     These verbs are collectively shown second-to-last in the context menu.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class AlternativeVerb : Verb
    {
        public override int TypePriority => 2;
        public new static string DefaultTextStyleClass = "AlternativeVerb";
        public override bool DefaultDoContactInteraction => true;

        public AlternativeVerb() : base()
        {
            TextStyleClass = DefaultTextStyleClass;
        }
    }

    /// <summary>
    ///    Activation-type verbs.
    /// </summary>
    /// <remarks>
    ///    These are verbs that activate an item in the world but are independent of the currently held items. For
    ///    example, opening a door or a GUI. These verbs should correspond to interactions that can be triggered by
    ///    using 'E', though many of those can also be triggered by left-mouse or 'Z' if there is no other interaction.
    ///    These verbs are collectively shown second in the context menu.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class ActivationVerb : Verb
    {
        public override int TypePriority => 1;
        public new static string DefaultTextStyleClass = "ActivationVerb";
        public override bool DefaultDoContactInteraction => true;

        public ActivationVerb() : base()
        {
            TextStyleClass = DefaultTextStyleClass;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ExamineVerb : Verb
    {
        public override int TypePriority => 0;
        public override bool CloseMenuDefault => false; // for examine verbs, this will close the examine tooltip.

        public bool ShowOnExamineTooltip = true;
    }

    /// <summary>
    ///     Verbs specifically for interactions that occur with equipped entities. These verbs should be accessible via
    ///     the stripping UI, and may optionally also be accessible via a verb on the equipee if the via inventory relay
    ///     events.get-verbs event.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class EquipmentVerb : Verb
    {
        public override int TypePriority => 5;
    }
}
