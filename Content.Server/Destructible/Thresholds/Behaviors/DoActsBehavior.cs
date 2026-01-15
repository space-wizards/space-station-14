using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class DoActsBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    /// <summary>
    ///     What acts should be triggered upon activation.
    /// </summary>
    [DataField]
    public ThresholdActs Acts { get; set; }

    public bool HasAct(ThresholdActs act)
    {
        return (Acts & act) != 0;
    }

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        if (HasAct(ThresholdActs.Breakage))
            _destructible.BreakEntity(owner);
        else if (HasAct(ThresholdActs.Destruction))
            _destructible.DestroyEntity(owner);
    }
}
