using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportV2
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 2;

    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = default!;
}
