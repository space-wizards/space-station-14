using Content.Shared.Ghost.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IIsGhostCondition : IHasComponentsCondition
{
    string[] IHasComponentsCondition.Components => [nameof(GhostComponent).Replace("Component", string.Empty)];
}
