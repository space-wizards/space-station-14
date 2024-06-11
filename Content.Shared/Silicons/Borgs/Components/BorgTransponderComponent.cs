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
    /// Popup shown to everyone when a borg is disabled.
    /// Gets passed a string "name".
    /// </summary>
    [DataField]
    public LocId DisabledPopup = "borg-transponder-disabled-popup";

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
}
