using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

// TODO ACTIONS make this a seprate component and remove the inheritance stuff.
// TODO ACTIONS convert to auto comp state?

// TODO add access attribute. Need to figure out what to do with decal & mapping actions.
// [Access(typeof(SharedActionsSystem))]
public abstract partial class BaseActionComponent : Component
{
    public abstract BaseActionEvent? BaseEvent { get; }

    /// <summary>
    ///     Icon representing this action in the UI.
    /// </summary>
    [DataField("icon")] public SpriteSpecifier? Icon;

    /// <summary>
    ///     For toggle actions only, icon to show when toggled on. If omitted, the action will simply be highlighted
    ///     when turned on.
    /// </summary>
    [DataField("iconOn")] public SpriteSpecifier? IconOn;

    /// <summary>
    ///     If not null, this color will modulate the action icon color.
    /// </summary>
    /// <remarks>
    ///     This currently only exists for decal-placement actions, so that the action icons correspond to the color of
    ///     the decal. But this is probably useful for other actions, including maybe changing color on toggle.
    /// </remarks>
    [DataField("iconColor")] public Color IconColor = Color.White;

    /// <summary>
    ///     Keywords that can be used to search for this action in the action menu.
    /// </summary>
    [DataField("keywords")] public HashSet<string> Keywords = new();

    /// <summary>
    ///     Whether this action is currently enabled. If not enabled, this action cannot be performed.
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;

    /// <summary>
    ///     The toggle state of this action. Toggling switches the currently displayed icon, see <see cref="Icon"/> and <see cref="IconOn"/>.
    /// </summary>
    /// <remarks>
    ///     The toggle can set directly via <see cref="SharedActionsSystem.SetToggled"/>, but it will also be
    ///     automatically toggled for targeted-actions while selecting a target.
    /// </remarks>
    [DataField]
    public bool Toggled;

    /// <summary>
    ///     The current cooldown on the action.
    /// </summary>
    // TODO serialization
    public (TimeSpan Start, TimeSpan End)? Cooldown;

    /// <summary>
    ///     Time interval between action uses.
    /// </summary>
    [DataField("useDelay")] public TimeSpan? UseDelay;

    /// <summary>
    ///     Convenience tool for actions with limited number of charges. Automatically decremented on use, and the
    ///     action is disabled when it reaches zero. Does NOT automatically remove the action from the action bar.
    ///     However, charges will regenerate if <see cref="RenewCharges"/> is enabled and the action will not disable
    ///     when charges reach zero.
    /// </summary>
    [DataField("charges")] public int? Charges;

    /// <summary>
    ///     The max charges this action has. If null, this is set automatically from <see cref="Charges"/> on mapinit.
    /// </summary>
    [DataField] public int? MaxCharges;

    /// <summary>
    ///     If enabled, charges will regenerate after a <see cref="Cooldown"/> is complete
    /// </summary>
    [DataField("renewCharges")]public bool RenewCharges;

    /// <summary>
    /// The entity that contains this action. If the action is innate, this may be the user themselves.
    /// This should almost always be non-null.
    /// </summary>
    [Access(typeof(ActionContainerSystem), typeof(SharedActionsSystem))]
    [DataField]
    public EntityUid? Container;

    /// <summary>
    ///     Entity to use for the action icon. If no entity is provided and the <see cref="Container"/> differs from
    ///     <see cref="AttachedEntity"/>, then it will default to using <see cref="Container"/>
    /// </summary>
    public EntityUid? EntityIcon
    {
        get
        {
            if (EntIcon != null)
                return EntIcon;

            if (AttachedEntity != Container)
                return Container;

            return null;
        }
        set => EntIcon = value;
    }

    [DataField]
    public EntityUid? EntIcon;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot currently interact. Some spells or
    ///     abilities may want to disable this and implement their own checks.
    /// </summary>
    [DataField("checkCanInteract")] public bool CheckCanInteract = true;

    /// <summary>
    /// Whether to check if the user is conscious or not. Can be used instead of <see cref="CheckCanInteract"/>
    /// for a more permissive check.
    /// </summary>
    [DataField] public bool CheckConsciousness = true;

    /// <summary>
    ///     If true, this will cause the action to only execute locally without ever notifying the server.
    /// </summary>
    [DataField("clientExclusive")] public bool ClientExclusive = false;

    /// <summary>
    ///     Determines the order in which actions are automatically added the action bar.
    /// </summary>
    [DataField("priority")] public int Priority = 0;

    /// <summary>
    ///     What entity, if any, currently has this action in the actions component?
    /// </summary>
    [DataField] public EntityUid? AttachedEntity;

    /// <summary>
    ///     If true, this will cause the the action event to always be raised directed at the action performer/user instead of the action's container/provider.
    /// </summary>
    [DataField]
    public bool RaiseOnUser;

    /// <summary>
    ///     Whether or not to automatically add this action to the action bar when it becomes available.
    /// </summary>
    [DataField("autoPopulate")] public bool AutoPopulate = true;

    /// <summary>
    ///     Temporary actions are deleted when they get removed a <see cref="ActionsComponent"/>.
    /// </summary>
    [DataField("temporary")] public bool Temporary;

    /// <summary>
    ///     Determines the appearance of the entity-icon for actions that are enabled via some entity.
    /// </summary>
    [DataField("itemIconStyle")] public ItemActionIconStyle ItemIconStyle;

    /// <summary>
    ///     If not null, this sound will be played when performing this action.
    /// </summary>
    [DataField("sound")] public SoundSpecifier? Sound;
}

[Serializable, NetSerializable]
public abstract class BaseActionComponentState : ComponentState
{
    public SpriteSpecifier? Icon;
    public SpriteSpecifier? IconOn;
    public Color IconColor;
    public HashSet<string> Keywords;
    public bool Enabled;
    public bool Toggled;
    public (TimeSpan Start, TimeSpan End)? Cooldown;
    public TimeSpan? UseDelay;
    public int? Charges;
    public int? MaxCharges;
    public bool RenewCharges;
    public NetEntity? Container;
    public NetEntity? EntityIcon;
    public bool CheckCanInteract;
    public bool CheckConsciousness;
    public bool ClientExclusive;
    public int Priority;
    public NetEntity? AttachedEntity;
    public bool RaiseOnUser;
    public bool AutoPopulate;
    public bool Temporary;
    public ItemActionIconStyle ItemIconStyle;
    public SoundSpecifier? Sound;

    protected BaseActionComponentState(BaseActionComponent component, IEntityManager entManager)
    {
        Container = entManager.GetNetEntity(component.Container);
        EntityIcon = entManager.GetNetEntity(component.EntIcon);
        AttachedEntity = entManager.GetNetEntity(component.AttachedEntity);
        RaiseOnUser = component.RaiseOnUser;
        Icon = component.Icon;
        IconOn = component.IconOn;
        IconColor = component.IconColor;
        Keywords = component.Keywords;
        Enabled = component.Enabled;
        Toggled = component.Toggled;
        Cooldown = component.Cooldown;
        UseDelay = component.UseDelay;
        Charges = component.Charges;
        MaxCharges = component.MaxCharges;
        RenewCharges = component.RenewCharges;
        CheckCanInteract = component.CheckCanInteract;
        CheckConsciousness = component.CheckConsciousness;
        ClientExclusive = component.ClientExclusive;
        Priority = component.Priority;
        AutoPopulate = component.AutoPopulate;
        Temporary = component.Temporary;
        ItemIconStyle = component.ItemIconStyle;
        Sound = component.Sound;
    }
}
