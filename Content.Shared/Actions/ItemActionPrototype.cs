using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Actions
{
    /// <summary>
    /// An action which is granted to an entity via an item (such as toggling a flashlight).
    /// </summary>
    [Prototype("itemAction")]
    public class ItemActionPrototype : BaseActionPrototype
    {
        /// <summary>
        /// Type of item action, no 2 itemAction prototypes should have the same one.
        /// </summary>
        public ItemActionType ActionType { get; private set; }

        /// <summary>
        /// The IInstantItemAction that should be invoked when performing this
        /// action. Null if this is not an Instant ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IInstantItemAction InstantAction { get; private set; }

        /// <summary>
        /// The IToggleItemAction that should be invoked when performing this
        /// action. Null if this is not a Toggle ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public IToggleItemAction ToggleAction { get; private set; }

        /// <summary>
        /// The ITargetEntityItemAction that should be invoked when performing this
        /// action. Null if this is not a TargetEntity ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetEntityItemAction TargetEntityAction { get; private set; }

        /// <summary>
        /// The ITargetPointItemAction that should be invoked when performing this
        /// action. Null if this is not a TargetPoint ActionBehaviorType.
        /// Will be null on client side if the behavior is not in Content.Client.
        /// </summary>
        public ITargetPointItemAction TargetPointAction { get; private set; }

        public new void LoadFrom(YamlMappingNode mapping)
        {
            base.LoadFrom(mapping);
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ActionType, "actionType", ItemActionType.Error);
            if (ActionType == ItemActionType.Error)
            {
                Logger.ErrorS("action", "missing or invalid actionType for action with name {0}", Name);
            }

            // TODO: Split this class into server/client after RobustToolbox#1405
            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;

            IActionBehavior behavior = null;
            serializer.DataField(ref behavior, "behavior", null);
            if (behavior == null)
            {
                BehaviorType = BehaviorType.None;
                Logger.ErrorS("action", "missing or invalid behavior for action with name {0}", Name);
            }
            else if (behavior is IInstantItemAction instantAction)
            {
                ValidateBehaviorType(BehaviorType.Instant, typeof(IInstantItemAction));
                BehaviorType = BehaviorType.Instant;
                InstantAction = instantAction;
            }
            else if (behavior is IToggleItemAction toggleAction)
            {
                ValidateBehaviorType(BehaviorType.Toggle, typeof(IToggleItemAction));
                BehaviorType = BehaviorType.Toggle;
                ToggleAction = toggleAction;
            }
            else if (behavior is ITargetEntityItemAction targetEntity)
            {
                ValidateBehaviorType(BehaviorType.TargetEntity, typeof(ITargetEntityItemAction));
                BehaviorType = BehaviorType.TargetEntity;
                TargetEntityAction = targetEntity;
            }
            else if (behavior is ITargetPointItemAction targetPointAction)
            {
                ValidateBehaviorType(BehaviorType.TargetPoint, typeof(ITargetPointItemAction));
                BehaviorType = BehaviorType.TargetPoint;
                TargetPointAction = targetPointAction;
            }
            else
            {
                BehaviorType = BehaviorType.None;
                Logger.ErrorS("action", "unrecognized behavior type for action with name {0}", Name);
            }
        }
    }
}
