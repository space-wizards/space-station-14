using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnCreamPie(Entity<CreamPiedComponent> entity, ref AdminOperationEvent<CreamPieOperation> args)
    {
        _creamPie.SetCreamPied(entity.AsNullable(), true);
    }
}
