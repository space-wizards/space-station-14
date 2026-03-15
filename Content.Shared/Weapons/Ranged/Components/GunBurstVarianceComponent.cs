using Content.Shared.Defects.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// Randomizes shots-per-burst on a per-burst basis. A burst-mode gun with this component
// fires a different number of rounds each trigger pull, within [MinShots, MaxShots].
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunBurstVarianceComponent : DefectComponent
{
    public GunBurstVarianceComponent()
    {
        Prob = 0.6f;
    }

    // Minimum shots per burst (inclusive).
    [DataField]
    public int MinShots = 1;

    // Maximum shots per burst (inclusive).
    [DataField]
    public int MaxShots = 8;

    // Currently rolled shots-per-burst value. Re-rolled after each burst completes.
    // Networked so clients can predict correctly.
    [DataField, AutoNetworkedField]
    public int CurrentShots;
}
