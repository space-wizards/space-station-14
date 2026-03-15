using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.RoundEnd;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

public sealed partial class OccursDuringRoundEndCondition : EntityTableCondition
{
    /// <summary>
    /// If false, the event won't trigger during ongoing evacuation.
    /// </summary>
    [DataField]
    public bool OccursDuringRoundEnd = true;

    private static SharedRoundEndSystem? _sharedRoundEnd;

    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        _sharedRoundEnd ??= entMan.System<SharedRoundEndSystem>();

        return OccursDuringRoundEnd || !_sharedRoundEnd.IsRoundEndRequested();
    }
}