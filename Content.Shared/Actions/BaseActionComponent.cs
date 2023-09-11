using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

// TODO this should be an IncludeDataFields of each action component type, not use inheritance
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
    public bool Toggled;

    /// <summary>
    ///     The current cooldown on the action.
    /// </summary>
    public (TimeSpan Start, TimeSpan End)? Cooldown;

    /// <summary>
    ///     Time interval between action uses.
    /// </summary>
    [DataField("useDelay")] public TimeSpan? UseDelay;

    /// <summary>
    ///     Convenience tool for actions with limited number of charges. Automatically decremented on use, and the
    ///     action is disabled when it reaches zero. Does NOT automatically remove the action from the action bar.
    /// </summary>
    [DataField("charges")] public int? Charges;

    /// <summary>
    ///     The entity that enables / provides this action. If the action is innate, this may be the user themselves. If
    ///     this action has no provider (e.g., mapping tools), the this will result in broadcast events.
    /// </summary>
    public EntityUid? Provider;

    /// <summary>
    ///     Entity to use for the action icon. Defaults to using <see cref="Provider"/>.
    /// </summary>
    public EntityUid? EntityIcon
    {
        get => _entityIcon ?? Provider;
        set => _entityIcon = value;
    }

    private EntityUid? _entityIcon;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot currently interact. Some spells or
    ///     abilities may want to disable this and implement their own checks.
    /// </summary>
    [DataField("checkCanInteract")] public bool CheckCanInteract = true;

    /// <summary>
    ///     If true, will simply execute the action locally without sending to the server.
    /// </summary>
    [DataField("clientExclusive")] public bool ClientExclusive = false;

    /// <summary>
    ///     Determines the order in which actions are automatically added the action bar.
    /// </summary>
    [DataField("priority")] public int Priority = 0;

    /// <summary>
    ///     What entity, if any, currently has this action in the actions component?
    /// </summary>
    [ViewVariables] public EntityUid? AttachedEntity;

    /// <summary>
    ///     Whether or not to automatically add this action to the action bar when it becomes available.
    /// </summary>
    [DataField("autoPopulate")] public bool AutoPopulate = true;


    /// <summary>
    ///     Whether or not to automatically remove this action to the action bar when it becomes unavailable.
    /// </summary>
    [DataField("autoRemove")] public bool AutoRemove = true;

    /// <summary>
    ///     Temporary actions are removed from the action component when removed from the action-bar/GUI. Currently,
    ///     should only be used for client-exclusive actions (server is not notified).
    /// </summary>
    /// <remarks>
    ///     Currently there is no way for a player to just voluntarily remove actions. They can hide them from the
    ///     toolbar, but not actually remove them. This is undesirable for things like dynamically added mapping
    ///     entity-selection actions, as the # of actions would just keep increasing.
    /// </remarks>
    [DataField("temporary")] public bool Temporary;
    // TODO re-add support for this
    // UI refactor seems to have just broken it.

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
    public NetEntity? Provider;
    public NetEntity? EntityIcon;
    public bool CheckCanInteract;
    public bool ClientExclusive;
    public int Priority;
    public NetEntity? AttachedEntity;
    public bool AutoPopulate;
    public bool AutoRemove;
    public bool Temporary;
    public ItemActionIconStyle ItemIconStyle;
    public SoundSpecifier? Sound;

    protected BaseActionComponentState(BaseActionComponent component, IEntityManager entManager)
    {
        Icon = component.Icon;
        IconOn = component.IconOn;
        IconColor = component.IconColor;
        Keywords = component.Keywords;
        Enabled = component.Enabled;
        Toggled = component.Toggled;
        Cooldown = component.Cooldown;
        UseDelay = component.UseDelay;
        Charges = component.Charges;

        // TODO ACTION REFACTOR fix bugs
        if (entManager.TryGetNetEntity(component.Provider, out var provider))
            Provider = provider;
        if (entManager.TryGetNetEntity(component.EntityIcon, out var icon))
            EntityIcon = icon;
        if (entManager.TryGetNetEntity(component.AttachedEntity, out var attached))
            AttachedEntity = attached;

        CheckCanInteract = component.CheckCanInteract;
        ClientExclusive = component.ClientExclusive;
        Priority = component.Priority;
        AutoPopulate = component.AutoPopulate;
        AutoRemove = component.AutoRemove;
        Temporary = component.Temporary;
        ItemIconStyle = component.ItemIconStyle;
        Sound = component.Sound;
    }
}
