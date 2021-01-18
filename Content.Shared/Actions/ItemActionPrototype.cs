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

        /// <see cref="ItemActionIconStyle"/>
        public ItemActionIconStyle IconStyle { get; private set; }

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

        public override void LoadFrom(YamlMappingNode mapping)
        {
            base.LoadFrom(mapping);
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ActionType, "actionType", ItemActionType.Error);
            if (ActionType == ItemActionType.Error)
            {
                Logger.ErrorS("action", "missing or invalid actionType for action with name {0}", Name);
            }

            serializer.DataField(this, x => x.IconStyle, "iconStyle", ItemActionIconStyle.BigItem);

            // TODO: Split this class into server/client after RobustToolbox#1405
            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;

            IItemActionBehavior behavior = null;
            serializer.DataField(ref behavior, "behavior", null);
            switch (behavior)
            {
                case null:
                    BehaviorType = BehaviorType.None;
                    Logger.ErrorS("action", "missing or invalid behavior for action with name {0}", Name);
                    break;
                case IInstantItemAction instantAction:
                    ValidateBehaviorType(BehaviorType.Instant, typeof(IInstantItemAction));
                    BehaviorType = BehaviorType.Instant;
                    InstantAction = instantAction;
                    break;
                case IToggleItemAction toggleAction:
                    ValidateBehaviorType(BehaviorType.Toggle, typeof(IToggleItemAction));
                    BehaviorType = BehaviorType.Toggle;
                    ToggleAction = toggleAction;
                    break;
                case ITargetEntityItemAction targetEntity:
                    ValidateBehaviorType(BehaviorType.TargetEntity, typeof(ITargetEntityItemAction));
                    BehaviorType = BehaviorType.TargetEntity;
                    TargetEntityAction = targetEntity;
                    break;
                case ITargetPointItemAction targetPointAction:
                    ValidateBehaviorType(BehaviorType.TargetPoint, typeof(ITargetPointItemAction));
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

    /// <summary>
    /// Determines how the action icon appears in the hotbar for item actions.
    /// </summary>
    public enum ItemActionIconStyle : byte
    {
        /// <summary>
        /// The default - the item icon will be big with a small action icon in the corner
        /// </summary>
        BigItem,
        /// <summary>
        /// The action icon will be big with a small item icon in the corner
        /// </summary>
        BigAction,
        /// <summary>
        /// BigAction but no item icon will be shown in the corner.
        /// </summary>
        NoItem
    }
}
