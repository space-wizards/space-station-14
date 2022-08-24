using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;

namespace Content.Server.Humanoid;

[RegisterComponent]
public sealed class HumanoidComponent : SharedHumanoidComponent
{
    public MarkingSet CurrentMarkings = new();

    /// <summary>
    ///     Any custom base layers this humanoid might have. See:
    ///     limb transplants (potentially), robotic arms, etc.
    ///     Stored on the server, this is merged in the client into
    ///     all layer settings.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    [DataField("alwaysEnsureDefault")] public bool AlwaysEnsureDefault;
}
