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
    [Prototype(("action"))]
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
        /// The type of behavior this action has. This is determined based
        /// on the subtype of IActionBehavior this action has for its behavior. This is just
        /// a more convenient way to check that.
        /// </summary>
        public ActionBehaviorType ActionBehaviorType { get; private set; }

        /// <summary>
        /// The IInstantAction that should be invoked when performing this
        /// action. Null if this is not an Instant ActionBehaviorType.
        /// </summary>
        public IInstantAction InstantAction { get; private set; }

        /// <summary>
        /// The IToggleAction that should be invoked when performing this
        /// action. Null if this is not a Toggle ActionBehaviorType.
        /// </summary>
        public IToggleAction ToggleAction { get; private set; }

        /// <summary>
        /// The ITargetEntityAction that should be invoked when performing this
        /// action. Null if this is not a TargetEntity ActionBehaviorType.
        /// </summary>
        public ITargetEntityAction TargetEntityAction { get; private set; }

        /// <summary>
        /// The ITargetPointAction that should be invoked when performing this
        /// action. Null if this is not a TargetPoint ActionBehaviorType.
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

            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;

            IActionBehavior behavior = null;
            serializer.DataField(ref behavior, "behavior", null);
            if (behavior == null)
            {
                ActionBehaviorType = ActionBehaviorType.None;
                Logger.WarningS("action", "missing or invalid behavior for action with name {0}", Name);
            }
            else if (behavior is IInstantAction instantAction)
            {
                ActionBehaviorType = ActionBehaviorType.Instant;
                InstantAction = instantAction;
            }
            else if (behavior is IToggleAction toggleAction)
            {
                ActionBehaviorType = ActionBehaviorType.Toggle;
                ToggleAction = toggleAction;
            }
            else if (behavior is ITargetEntityAction targetEntity)
            {
                ActionBehaviorType = ActionBehaviorType.TargetEntity;
                TargetEntityAction = targetEntity;
            }
            else if (behavior is ITargetPointAction targetPointAction)
            {
                ActionBehaviorType = ActionBehaviorType.TargetPoint;
                TargetPointAction = targetPointAction;
            }
            else
            {
                ActionBehaviorType = ActionBehaviorType.None;
                Logger.WarningS("action", "unrecognized behavior type for action with name {0}", Name);
            }
        }
    }

    /// <summary>
    /// The behavior / logic of the action. Each of these corresponds to a particular IActionBehavior
    /// interface.
    /// </summary>
    public enum ActionBehaviorType
    {
        /// <summary>
        /// Action doesn't do anything.
        /// </summary>
        None,

        /// <summary>
        /// Action which does something immediately when used and has
        /// no target.
        /// </summary>
        Instant,

        /// <summary>
        /// Action which can be toggled on and off
        /// </summary>
        Toggle,

        /// <summary>
        /// Action which is used on a targeted entity.
        /// </summary>
        TargetEntity,

        /// <summary>
        /// Action which requires the user to select a target point, which
        /// does not necessarily have an entity on it.
        /// </summary>
        TargetPoint
    }
}
