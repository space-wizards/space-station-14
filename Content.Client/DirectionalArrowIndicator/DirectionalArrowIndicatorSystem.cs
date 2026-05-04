using Content.Shared.DirectionalArrowIndicator;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.DirectionalArrowIndicator;

public sealed class DirectionalArrowIndicatorSystem : EntitySystem
{
    private static readonly EntProtoId ExamineArrow = "DirectionalArrowIndicator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DirectionalArrowIndicatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DirectionalArrowIndicatorComponent> ent, ref ExaminedEvent args)
    {
        Spawn(ExamineArrow, new EntityCoordinates(ent, 0, 0));
    }
}
