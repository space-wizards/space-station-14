using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Actions.ActionTypes;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract class ActionType : IEquatable<ActionType>, IComparable, ICloneable
{
    /// <summary>
    ///     Icon representing this action in the UI.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon;

    /// <summary>
    ///     For toggle actions only, icon to show when toggled on. If omitted, the action will simply be highlighted
    ///     when turned on.
    /// </summary>
    [DataField("iconOn")]
    public SpriteSpecifier? IconOn;

    /// <summary>
    ///     If not null, this color will modulate the action icon color.
    /// </summary>
    /// <remarks>
    ///     This currently only exists for decal-placement actions, so that the action icons correspond to the color of
    ///     the decal. But this is probably useful for other actions, including maybe changing color on toggle.
    /// </remarks>
    [DataField("iconColor")]
    public Color IconColor = Color.White;

    /// <summary>
    ///     Name to show in UI.
    /// </summary>
    [DataField("name")]
    public string DisplayName = string.Empty;

    /// <summary>
    ///     Description to show in UI. Accepts formatting.
    /// </summary>
    [DataField("description")]
    public string Description = string.Empty;

    /// <summary>
    ///     Keywords that can be used to search for this action in the action menu.
    /// </summary>
    [DataField("keywords")]
    public HashSet<string> Keywords = new();

    /// <summary>
    ///     Whether this action is currently enabled. If not enabled, this action cannot be performed.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    ///     The toggle state of this action. Toggling switches the currently displayed icon, see <see cref="Icon"/> and <see cref="IconOn"/>.
    /// </summary>
    /// <remarks>
    ///     The toggle can set directly via <see cref="SharedActionsSystem.SetToggled()"/>, but it will also be
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
    [DataField("useDelay")]
    public TimeSpan? UseDelay;

    /// <summary>
    ///     Convenience tool for actions with limited number of charges. Automatically decremented on use, and the
    ///     action is disabled when it reaches zero. Does NOT automatically remove the action from the action bar.
    /// </summary>
    [DataField("charges")]
    public int? Charges;

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
    [DataField("checkCanInteract")]
    public bool CheckCanInteract = true;

    /// <summary>
    ///     If true, will simply execute the action locally without sending to the server.
    /// </summary>
    [DataField("clientExclusive")]
    public bool ClientExclusive = false;

    /// <summary>
    ///     Determines the order in which actions are automatically added the action bar.
    /// </summary>
    [DataField("priority")]
    public int Priority = 0;

    /// <summary>
    ///     What entity, if any, currently has this action in the actions component?
    /// </summary>
    [ViewVariables]
    public EntityUid? AttachedEntity;

    /// <summary>
    ///     Whether or not to automatically add this action to the action bar when it becomes available.
    /// </summary>
    [DataField("autoPopulate")]
    public bool AutoPopulate = true;


    /// <summary>
    ///     Whether or not to automatically remove this action to the action bar when it becomes unavailable.
    /// </summary>
    [DataField("autoRemove")]
    public bool AutoRemove = true;

    /// <summary>
    ///     Temporary actions are removed from the action component when removed from the action-bar/GUI. Currently,
    ///     should only be used for client-exclusive actions (server is not notified).
    /// </summary>
    /// <remarks>
    ///     Currently there is no way for a player to just voluntarily remove actions. They can hide them from the
    ///     toolbar, but not actually remove them. This is undesirable for things like dynamically added mapping
    ///     entity-selection actions, as the # of actions would just keep increasing.
    /// </remarks>
    [DataField("temporary")]
    public bool Temporary;

    /// <summary>
    ///     Determines the appearance of the entity-icon for actions that are enabled via some entity.
    /// </summary>
    [DataField("itemIconStyle")]
    public ItemActionIconStyle ItemIconStyle;

    /// <summary>
    ///     If not null, the user will speak these words when performing the action. Convenient feature to have for some
    ///     actions. Gets passed through localization.
    /// </summary>
    [DataField("speech")]
    public string? Speech;

    /// <summary>
    ///     If not null, this sound will be played when performing this action.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;

    [DataField("audioParams")]
    public AudioParams? AudioParams;

    /// <summary>
    ///     A pop-up to show the user when performing this action. Gets passed through localization.
    /// </summary>
    [DataField("userPopup")]
    public string? UserPopup;

    /// <summary>
    ///     A pop-up to show to all players when performing this action. Gets passed through localization.
    /// </summary>
    [DataField("popup")]
    public string? Popup;

    /// <summary>
    ///     If not null, this string will be appended to the pop-up localization strings when the action was toggled on
    ///     after execution. Exists to make it easy to have a different pop-up for turning the action on or off (e.g.,
    ///     combat mode toggle).
    /// </summary>
    [DataField("popupToggleSuffix")]
    public string? PopupToggleSuffix = null;

    /// <summary>
    ///     Compares two actions based on their properties. This is used to determine equality when the client requests the
    ///     server to perform some action. Also determines the order in which actions are automatically added to the action bar.
    /// </summary>
    /// <remarks>
    ///     Basically: if an action has the same priority, name, and is enabled by the same entity, then the actions are considered equal.
    ///     The entity-check is required to avoid toggling all flashlights simultaneously whenever a flashlight-hoarder uses an action.
    /// </remarks>
    public virtual int CompareTo(object? obj)
    {
        if (obj is not ActionType otherAction)
            return -1;

        if (Priority != otherAction.Priority)
            return otherAction.Priority - Priority;

        var name = FormattedMessage.RemoveMarkup(Loc.GetString(DisplayName));
        var otherName = FormattedMessage.RemoveMarkup(Loc.GetString(otherAction.DisplayName));
        if (name != otherName)
            return string.Compare(name, otherName, StringComparison.CurrentCulture);

        if (Provider != otherAction.Provider)
        {
            if (Provider == null)
                return -1;

            if (otherAction.Provider == null)
                return 1;

            // uid to int casting... it says "Do NOT use this in content". You can't tell me what to do.
            return (int) Provider - (int) otherAction.Provider;
        }

        return 0;
    }

    /// <summary>
    ///     Proper client-side state handling requires the ability to clone an action from the component state.
    ///     Otherwise modifying the action can lead to modifying the stored server state.
    /// </summary>
    public abstract object Clone();

    public virtual void CopyFrom(object objectToClone)
    {
        if (objectToClone is not ActionType toClone)
            return;

        // This is pretty Ugly to look at. But actions are sent to the client in a component state, so they have to be
        // cloneable. Would be easy if this were a struct of only value-types, but I don't want to restrict actions like
        // that.
        Priority = toClone.Priority;
        Icon = toClone.Icon;
        IconOn = toClone.IconOn;
        DisplayName = toClone.DisplayName;
        Description = toClone.Description;
        Provider = toClone.Provider;
        AttachedEntity = toClone.AttachedEntity;
        Enabled = toClone.Enabled;
        Toggled = toClone.Toggled;
        Cooldown = toClone.Cooldown;
        Charges = toClone.Charges;
        Keywords = new(toClone.Keywords);
        AutoPopulate = toClone.AutoPopulate;
        AutoRemove = toClone.AutoRemove;
        ItemIconStyle = toClone.ItemIconStyle;
        CheckCanInteract = toClone.CheckCanInteract;
        Speech = toClone.Speech;
        UseDelay = toClone.UseDelay;
        Sound = toClone.Sound;
        AudioParams = toClone.AudioParams;
        UserPopup = toClone.UserPopup;
        Popup = toClone.Popup;
        PopupToggleSuffix = toClone.PopupToggleSuffix;
        ItemIconStyle = toClone.ItemIconStyle;
        _entityIcon = toClone._entityIcon;
    }

    public bool Equals(ActionType? other)
    {
        return CompareTo(other) == 0;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Priority.GetHashCode();
            hashCode = (hashCode * 397) ^ DisplayName.GetHashCode();
            hashCode = (hashCode * 397) ^ Provider.GetHashCode();
            return hashCode;
        }
    }
}
