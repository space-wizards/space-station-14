using Content.Server.Ame;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class OverloadAmeConditionSystem : EntitySystem
{
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<AmeExplodedEvent>(OnAmeExploded);
    }

    private void OnAmeExploded(AmeExplodedEvent args)
    {
        var query = EntityQueryEnumerator<OverloadAmeConditionComponent, CodeConditionComponent>();
        while (query.MoveNext(out var uid, out var ameCondition, out var codeCondition))
        {
            _codeCondition.SetCompleted((uid, codeCondition));
        }
    }
}
