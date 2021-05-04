#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Base class for action prototypes.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class BaseActionPrototype : IPrototype, ISerializationHooks
    {
        public abstract string ID { get; }

        /// <summary>
        /// Icon representing this action in the UI.
        /// </summary>
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// For toggle actions only, icon to show when toggled on. If omitted,
        /// the action will simply be highlighted when turned on.
        /// </summary>
        [ViewVariables]
        [DataField("iconOn")]
        public SpriteSpecifier IconOn { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// Name to show in UI. Accepts formatting.
        /// </summary>
        [DataField("name")]
        public FormattedMessage Name { get; private set; } = new();

        /// <summary>
        /// Description to show in UI. Accepts formatting.
        /// </summary>
        [DataField("description")]
        public FormattedMessage Description { get; } = new();

        /// <summary>
        /// Requirements message to show in UI. Accepts formatting, but generally should be avoided
        /// so the requirements message isn't too prominent in the tooltip.
        /// </summary>
        [DataField("requires")]
        public string Requires { get; } = string.Empty;

        /// <summary>
        /// The type of behavior this action has. This is valid clientside and serverside.
        /// </summary>
        [DataField("behaviorType")]
        public BehaviorType BehaviorType { get; protected set; } = BehaviorType.None;

        /// <summary>
        /// For targetpoint or targetentity actions, if this is true the action will remain
        /// selected after it is used, so it can be continuously re-used. If this is false,
        /// the action will be deselected after one use.
        /// </summary>
        [DataField("repeat")]
        public bool Repeat { get; }

        /// <summary>
        /// For TargetEntity/TargetPoint actions, should the action be de-selected if currently selected (choosing a target)
        /// when it goes on cooldown. Defaults to false.
        /// </summary>
        [DataField("deselectOnCooldown")]
        public bool DeselectOnCooldown { get; }

        /// <summary>
        /// For TargetEntity actions, should the action be de-selected if the user doesn't click an entity when
        /// selecting a target. Defaults to false.
        /// </summary>
        [DataField("deselectWhenEntityNotClicked")]
        public bool DeselectWhenEntityNotClicked { get; }

        [DataField("filters")] private List<string> _filters = new();

        /// <summary>
        /// Filters that can be used to filter this item in action menu.
        /// </summary>
        public IEnumerable<string> Filters => _filters;

        [DataField("keywords")] private List<string> _keywords = new();

        /// <summary>
        /// Keywords that can be used to search this item in action menu.
        /// </summary>
        public IEnumerable<string> Keywords => _keywords;

        /// <summary>
        /// True if this is an action that requires selecting a target
        /// </summary>
        public bool IsTargetAction =>
            BehaviorType == BehaviorType.TargetEntity || BehaviorType == BehaviorType.TargetPoint;

        public virtual void AfterDeserialization()
        {
            Name = new FormattedMessage();
            Name.AddText(ID);

            if (BehaviorType == BehaviorType.None)
            {
                Logger.ErrorS("action", "Missing behaviorType for action with name {0}", Name);
            }

            if (BehaviorType != BehaviorType.Toggle && IconOn != SpriteSpecifier.Invalid)
            {
                Logger.ErrorS("action", "for action {0}, iconOn was specified but behavior" +
                                        " type was {1}. iconOn is only supported for Toggle behavior type.", Name);
            }

            if (Repeat && BehaviorType != BehaviorType.TargetEntity && BehaviorType != BehaviorType.TargetPoint)
            {
                Logger.ErrorS("action", " action named {0} used repeat: true, but this is only supported for" +
                                        " TargetEntity and TargetPoint behaviorType and its behaviorType is {1}",
                    Name, BehaviorType);
            }
        }

        protected void ValidateBehaviorType(BehaviorType expected, Type actualInterface)
        {
            if (BehaviorType != expected)
            {
                Logger.ErrorS("action", "for action named {0}, behavior implements " +
                                        "{1}, so behaviorType should be {2} but was {3}", Name, actualInterface.Name, expected, BehaviorType);
            }
        }
    }

    /// <summary>
    /// The behavior / logic of the action. Each of these corresponds to a particular IActionBehavior
    /// (for actions) or IItemActionBehavior (for item actions)
    /// interface. Corresponds to action.behaviorType in YAML
    /// </summary>
    public enum BehaviorType
    {
        /// <summary>
        /// Action doesn't do anything.
        /// </summary>
        None,

        /// <summary>
        /// IInstantAction/IInstantItemAction. Action which does something immediately when used and has
        /// no target.
        /// </summary>
        Instant,

        /// <summary>
        /// IToggleAction/IToggleItemAction Action which can be toggled on and off
        /// </summary>
        Toggle,

        /// <summary>
        /// ITargetEntityAction/ITargetEntityItemAction. Action which is used on a targeted entity.
        /// </summary>
        TargetEntity,

        /// <summary>
        /// ITargetPointAction/ITargetPointItemAction. Action which requires the user to select a target point, which
        /// does not necessarily have an entity on it.
        /// </summary>
        TargetPoint
    }
}
