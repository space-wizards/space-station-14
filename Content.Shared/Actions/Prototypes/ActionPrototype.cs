using Content.Shared.Actions.Behaviors;
using Content.Shared.Module;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Actions.Prototypes
{
    /// <summary>
    /// An action which is granted directly to an entity (such as an innate ability
    /// or skill).
    /// </summary>
    [Prototype("action")]
    [DataDefinition]
    public sealed class ActionPrototype : BaseActionPrototype, ISerializationHooks
    {
        /// <summary>
        /// Type of action, no 2 action prototypes should have the same one.
        /// </summary>
        [DataField("actionType", required: true)]
        public ActionType ActionType { get; set; }

        [DataField("behavior", serverOnly: true)]
        private IActionBehavior? Behavior { get; set; }

        /// <summary>
        /// The IInstantAction that should be invoked when performing this
        /// action. Null if this is not an Instant ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IInstantAction InstantAction { get; private set; } = default!;

        /// <summary>
        /// The IToggleAction that should be invoked when performing this
        /// action. Null if this is not a Toggle ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IToggleAction ToggleAction { get; private set; } = default!;

        /// <summary>
        /// The ITargetEntityAction that should be invoked when performing this
        /// action. Null if this is not a TargetEntity ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetEntityAction TargetEntityAction { get; private set; } = default!;

        /// <summary>
        /// The ITargetPointAction that should be invoked when performing this
        /// action. Null if this is not a TargetPoint ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetPointAction TargetPointAction { get; private set; } = default!;

        public override string ID => ActionType.ToString();

        void ISerializationHooks.AfterDeserialization()
        {
            base.AfterDeserialization();

            if (ActionType == ActionType.Error)
            {
                Logger.ErrorS("action", "missing or invalid actionType for action with name {0}", Name);
            }

            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;

            switch (Behavior)
            {
                case null:
                    BehaviorType = BehaviorType.None;
                    Logger.ErrorS("action", "missing or invalid behavior for action with name {0}", Name);
                    break;
                case IInstantAction instantAction:
                    ValidateBehaviorType(BehaviorType.Instant, typeof(IInstantAction));
                    BehaviorType = BehaviorType.Instant;
                    InstantAction = instantAction;
                    break;
                case IToggleAction toggleAction:
                    ValidateBehaviorType(BehaviorType.Toggle, typeof(IToggleAction));
                    BehaviorType = BehaviorType.Toggle;
                    ToggleAction = toggleAction;
                    break;
                case ITargetEntityAction targetEntity:
                    ValidateBehaviorType(BehaviorType.TargetEntity, typeof(ITargetEntityAction));
                    BehaviorType = BehaviorType.TargetEntity;
                    TargetEntityAction = targetEntity;
                    break;
                case ITargetPointAction targetPointAction:
                    ValidateBehaviorType(BehaviorType.TargetPoint, typeof(ITargetPointAction));
                    BehaviorType = BehaviorType.TargetPoint;
                    TargetPointAction = targetPointAction;
                    break;
                default:
                    BehaviorType = BehaviorType.None;
                    Logger.ErrorS("action", "unrecognized behavior type for action with name {0}", Name);
                    break;
            }
        }
    }
}
