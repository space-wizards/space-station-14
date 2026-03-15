using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Component all actions are required to have.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[AutoGenerateComponentState(true, true)]
[EntityCategory("Actions")]
public sealed partial class ActionComponent : Component
{
    /// <summary>
    ///     Icon representing this action in the UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon;

    /// <summary>
    ///     For toggle actions only, icon to show when toggled on. If omitted, the action will simply be highlighted
    ///     when turned on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? IconOn;

    /// <summary>
    ///     For toggle actions only, background to show when toggled on.
    /// </summary>
    [DataField]
    public SpriteSpecifier? BackgroundOn;

    /// <summary>
    ///     If not null, this color will modulate the action icon color.
    /// </summary>
    /// <remarks>
    ///     This currently only exists for decal-placement actions, so that the action icons correspond to the color of
    ///     the decal. But this is probably useful for other actions, including maybe changing color on toggle.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Color IconColor = Color.White;

    /// <summary>
    ///     The original <see cref="IconColor"/> this action was.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color OriginalIconColor;

    /// <summary>
    ///     The color the action should turn to when disabled
    /// </summary>
    [DataField] public Color DisabledIconColor = Color.DimGray;

    /// <summary>
    ///     Keywords that can be used to search for this action in the action menu.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Keywords = new();

    /// <summary>
    ///     Whether this action is currently enabled. If not enabled, this action cannot be performed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    ///     The toggle state of this action. Toggling switches the currently displayed icon, see <see cref="Icon"/> and <see cref="IconOn"/>.
    /// </summary>
    /// <remarks>
    ///     The toggle can set directly via <see cref="SharedActionsSystem.SetToggled"/>, but it will also be
    ///     automatically toggled for targeted-actions while selecting a target.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Toggled;

    /// <summary>
    ///     The current cooldown on the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ActionCooldown? Cooldown;

    /// <summary>
    ///     If true, the action will have an initial cooldown applied upon addition.
    /// </summary>
    [DataField] public bool StartDelay = false;

    /// <summary>
    ///     Time interval between action uses.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? UseDelay;

    /// <summary>
    /// The entity that contains this action. If the action is innate, this may be the user themselves.
    /// This should almost always be non-null.
    /// </summary>
    [Access(typeof(ActionContainerSystem), typeof(SharedActionsSystem))]
    [DataField, AutoNetworkedField]
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

    [DataField, AutoNetworkedField]
    public EntityUid? EntIcon;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot currently interact. Some spells or
    ///     abilities may want to disable this and implement their own checks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckCanInteract = true;

    /// <summary>
    /// Whether to check if the user is conscious or not. Can be used instead of <see cref="CheckCanInteract"/>
    /// for a more permissive check.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckConsciousness = true;

    /// <summary>
    ///     If true, this will cause the action to only execute locally without ever notifying the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ClientExclusive;

    /// <summary>
    ///     Determines the order in which actions are automatically added the action bar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority = 0;

    /// <summary>
    ///     What entity, if any, currently has this action in the actions component?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttachedEntity;

    /// <summary>
    ///     If true, this will cause the the action event to always be raised directed at the action performer/user instead of the action's container/provider.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RaiseOnUser;

    /// <summary>
    ///     If true, this will cause the the action event to always be raised directed at the action itself instead of the action's container/provider.
    ///     Takes priority over RaiseOnUser.
    /// </summary>
    [DataField]
    [Obsolete("This datafield will be reworked in an upcoming action refactor")]
    public bool RaiseOnAction;

    /// <summary>
    ///     Whether or not to automatically add this action to the action bar when it becomes available.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoPopulate = true;

    /// <summary>
    ///     Temporary actions are deleted when they get removed a <see cref="ActionsComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Temporary;

    /// <summary>
    ///     Determines the appearance of the entity-icon for actions that are enabled via some entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ItemActionIconStyle ItemIconStyle;

    /// <summary>
    ///     If not null, this sound will be played when performing this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}

[DataRecord, Serializable, NetSerializable]
public partial record struct ActionCooldown
{
    [DataField(required: true, customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Start;

    [DataField(required: true, customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan End;
}
