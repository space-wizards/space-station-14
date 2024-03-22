using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Objectives.Components;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class ConsumeNutrientsObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConsumeNutrientsConditionComponent, ObjectiveGetProgressEvent>(OnConsumeNutrientsGetProgress);
    }

    private static void OnConsumeNutrientsGetProgress(EntityUid uid, ConsumeNutrientsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.NutrientsConsumed / comp.NutrientsRequired;
    }
}
