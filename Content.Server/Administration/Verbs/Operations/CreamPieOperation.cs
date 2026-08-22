using Content.Shared.Nutrition.Components;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnCreamPie(Entity<CreamPiedComponent> entity, ref AdminOperationEvent<CreamPieOperation> args)
    {
        _creamPie.SetCreamPied(entity.AsNullable(), true);
    }
}

// TODO: Use EntityEffectsOperation once CreamPied state has an entity effect.
public sealed partial class CreamPieOperation : AdminOperationBase<CreamPieOperation>;
