using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Enums;

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

    /// <summary>
    ///     Permanently hidden layers. Attempting to toggle these temporarily will not work.
    /// </summary>
    public HashSet<HumanoidVisualLayers> PermanentlyHidden = new();

    // Couldn't these be somewhere else?
    [ViewVariables] public Gender Gender = default!;
    [ViewVariables] public int Age = HumanoidCharacterProfile.MinimumAge;
}
