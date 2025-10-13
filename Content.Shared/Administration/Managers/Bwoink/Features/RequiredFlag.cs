namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// If this flag is present on a bwoinkchannel, the admin flag set in this feature will be required in order to "manage" a channel.
/// </summary>
public sealed partial class RequiredFlags : BwoinkChannelFeature
{
    [DataField(required: true)]
    public AdminFlags Flags { get; set; } = AdminFlags.Adminhelp;
}
