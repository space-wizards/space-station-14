using System;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Log;

namespace Content.Shared.Actions
{
    /// <summary>
    /// An action which appears in the action hotbar.
    /// </summary>
    [Prototype("action")]
    public class ActionPrototype : IPrototype
    {
        /// <summary>
        /// Type of action, no 2 action prototypes should have the same one.
        /// </summary>
        public ActionType ActionType { get; private set; }

        /// <summary>
        /// Icon representing this action in the UI.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon { get; private set; }

        /// <summary>
        /// Name to show in UI. Accepts formatting.
        /// </summary>
        public FormattedMessage Name { get; private set; }

        /// <summary>
        /// Description to show in UI. Accepts formatting.
        /// </summary>
        public FormattedMessage Description { get; private set; }

        /// <summary>
        /// Requirements message to show in UI. Does NOT accept formatting.
        /// </summary>
        public string Requires { get; private set; }

        /// <summary>
        /// The type of behavior this action has. This is valid clientside and serverside.
        /// </summary>
        public BehaviorType BehaviorType { get; private set; }

        /// <summary>
        /// The IInstantAction that should be invoked when performing this
        /// action. Null if this is not an Instant ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IInstantAction InstantAction { get; private set; }

        /// <summary>
        /// The IToggleAction that should be invoked when performing this
        /// action. Null if this is not a Toggle ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IToggleAction ToggleAction { get; private set; }

        /// <summary>
        /// The ITargetEntityAction that should be invoked when performing this
        /// action. Null if this is not a TargetEntity ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetEntityAction TargetEntityAction { get; private set; }

        /// <summary>
        /// The ITargetPointAction that should be invoked when performing this
        /// action. Null if this is not a TargetPoint ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetPointAction TargetPointAction { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataReadFunction("name", string.Empty,
                s => Name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => Description = FormattedMessage.FromMarkup(s));

            serializer.DataField(this, x => x.Requires,"requires", null);
            serializer.DataField(this, x => x.Icon,"icon", SpriteSpecifier.Invalid);

            serializer.DataField(this, x => x.ActionType, "actionType", ActionType.Error);
            if (ActionType == ActionType.Error)
            {
                Logger.WarningS("action", "missing or invalid actionType for action with name {0}", Name);
            }

            // client needs to know what type of behavior it is even if the actual implementation is only
            // on server side. If we wanted to avoid this we'd need to always add a shared or clientside interface
            // for each action even if there was only server-side logic, which would be cumbersome
            serializer.DataField(this, x => x.BehaviorType, "behaviorType", BehaviorType.None);
            if (BehaviorType == BehaviorType.None)
            {
                Logger.WarningS("action", "Missing behaviorType for action with name {0}", Name);
            }

            // TODO: Split this class into server/client after RobustToolbox#1405
            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;

            IActionBehavior behavior = null;
            serializer.DataField(ref behavior, "behavior", null);
            if (behavior == null)
            {
                BehaviorType = BehaviorType.None;
                Logger.WarningS("action", "missing or invalid behavior for action with name {0}", Name);
            }
            else if (behavior is IInstantAction instantAction)
            {
                ValidateBehaviorType(BehaviorType.Instant, typeof(IInstantAction));
                BehaviorType = BehaviorType.Instant;
                InstantAction = instantAction;
            }
            else if (behavior is IToggleAction toggleAction)
            {
                ValidateBehaviorType(BehaviorType.Toggle, typeof(IToggleAction));
                BehaviorType = BehaviorType.Toggle;
                ToggleAction = toggleAction;
            }
            else if (behavior is ITargetEntityAction targetEntity)
            {
                ValidateBehaviorType(BehaviorType.TargetEntity, typeof(ITargetEntityAction));
                BehaviorType = BehaviorType.TargetEntity;
                TargetEntityAction = targetEntity;
            }
            else if (behavior is ITargetPointAction targetPointAction)
            {
                ValidateBehaviorType(BehaviorType.TargetPoint, typeof(ITargetPointAction));
                BehaviorType = BehaviorType.TargetPoint;
                TargetPointAction = targetPointAction;
            }
            else
            {
                BehaviorType = BehaviorType.None;
                Logger.WarningS("action", "unrecognized behavior type for action with name {0}", Name);
            }
        }

        private void ValidateBehaviorType(BehaviorType expected, Type actualInterface)
        {
            if (BehaviorType != expected)
            {
                Logger.WarningS("action", "for action named {0}, behavior implements " +
                                          "{1}, so behaviorType should be {2} but was {3}", Name, actualInterface.Name, expected, BehaviorType);
            }
        }
    }



    /// <summary>
    /// The behavior / logic of the action. Each of these corresponds to a particular IActionBehavior
    /// interface. Corresponds to action.behaviorType in YAML
    /// </summary>
    public enum BehaviorType
    {
        /// <summary>
        /// Action doesn't do anything.
        /// </summary>
        None,

        /// <summary>
        /// IInstantAction. Action which does something immediately when used and has
        /// no target.
        /// </summary>
        Instant,

        /// <summary>
        /// IToggleAction Action which can be toggled on and off
        /// </summary>
        Toggle,

        /// <summary>
        /// ITargetEntityAction. Action which is used on a targeted entity.
        /// </summary>
        TargetEntity,

        /// <summary>
        /// ITargetPointAction. Action which requires the user to select a target point, which
        /// does not necessarily have an entity on it.
        /// </summary>
        TargetPoint
    }
}
