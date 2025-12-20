using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed partial class ModifiedMobThresholdsStatusEffectComponent : Component
{
    [DataField]
    public SortedDictionary<FixedPoint2, MobState> NewThresholds = [];

    [DataField]
    public SortedDictionary<FixedPoint2, MobState> OldThresholds = [];
}
