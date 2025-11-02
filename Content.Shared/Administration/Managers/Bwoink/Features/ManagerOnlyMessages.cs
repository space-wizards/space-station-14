namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// Allows managers to send messages that only other managers can see.
/// </summary>
public sealed partial class ManagerOnlyMessages : BwoinkChannelFeature
{
    /// <summary>
    /// The name that will be displayed next to the checkbox.
    /// </summary>
    [DataField]
    public LocId CheckName { get; set; } = "admin-ahelp-manager-only";

    /// <summary>
    /// The prefix to use when a message is manager only.
    /// </summary>
    [DataField]
    public LocId Prefix { get; set; } = "bwoink-message-manager-only";

    /// <summary>
    /// The tooltip to display when hovering over the checkbox.
    /// </summary>
    [DataField]
    public LocId ToolTip { get; set; } = "admin-ahelp-manager-only-tooltip";
}
