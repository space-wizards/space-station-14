#nullable enable
using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    public class SmokeAreaReactionEffect : AreaReactionEffect
    {
        protected override SolutionAreaEffectComponent? GetAreaEffectComponent(IEntity entity)
        {
            return entity.GetComponentOrNull<SmokeSolutionAreaEffectComponent>();
        }
    }
}
