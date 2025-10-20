namespace Content.Shared.Administration.Managers.Bwoink.Requirements;

/// <summary>
/// This requirement checks if the client has the required admin flag.
/// </summary>
public sealed partial class AdminFlagRequirement : BwoinkChannelCondition
{
    [DataField(required: true)]
    public AdminFlags Flags { get; set; } = AdminFlags.Adminhelp;
}
