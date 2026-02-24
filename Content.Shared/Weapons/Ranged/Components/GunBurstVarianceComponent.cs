using Content.Shared.Defects.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Randomizes shots-per-burst on a per-burst basis. A burst-mode gun with this component
/// fires a different number of rounds each trigger pull, within [MinShots, MaxShots].
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunBurstVarianceComponent : DefectComponent
{
    public GunBurstVarianceComponent()
    {
        Prob = 0.6f;
    }

    /// <summary>Minimum shots per burst (inclusive).</summary>
    [DataField]
    public int MinShots = 1;

    /// <summary>Maximum shots per burst (inclusive).</summary>
    [DataField]
    public int MaxShots = 8;

    /// <summary>
    /// Currently rolled shots-per-burst value. Re-rolled after each burst completes.
    /// Networked so clients can predict correctly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentShots;
}
