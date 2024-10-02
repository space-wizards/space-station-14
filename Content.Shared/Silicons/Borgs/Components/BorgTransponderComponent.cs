using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// Periodically broadcasts borg data to robotics consoles.
/// When not emagged, handles disabling and destroying commands as expected.
/// </summary>
[RegisterComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class BorgTransponderComponent : Component
{
    /// <summary>
    /// Sprite of the chassis to send.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier? Sprite;

    /// <summary>
    /// Name of the chassis to send.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Popup shown to everyone after a borg is disabled.
    /// Gets passed a string "name".
    /// </summary>
    [DataField]
    public LocId DisabledPopup = "borg-transponder-disabled-popup";

    /// <summary>
    /// Popup shown to the borg when it is being disabled.
    /// </summary>
    [DataField]
    public LocId DisablingPopup = "borg-transponder-disabling-popup";

    /// <summary>
    /// Popup shown to everyone when a borg is being destroyed.
    /// Gets passed a string "name".
    /// </summary>
    [DataField]
    public LocId DestroyingPopup = "borg-transponder-destroying-popup";

    /// <summary>
    /// How long to wait between each broadcast.
    /// </summary>
    [DataField]
    public TimeSpan BroadcastDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When to next broadcast data.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextBroadcast = TimeSpan.Zero;

    /// <summary>
    /// When to next disable the borg.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextDisable;

    /// <summary>
    /// How long to wait to disable the borg after RD has ordered it.
    /// </summary>
    [DataField]
    public TimeSpan DisableDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Pretend that the borg cannot be disabled due to being on delay.
    /// </summary>
    [DataField]
    public bool FakeDisabling;

    /// <summary>
    /// Pretend that the borg has no brain inserted.
    /// </summary>
    [DataField]
    public bool FakeDisabled;
}
