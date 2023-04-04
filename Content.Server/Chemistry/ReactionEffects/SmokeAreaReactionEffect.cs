using Content.Server.Chemistry.Components;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SmokeAreaReactionEffect : AreaReactionEffect
    {
        protected override SmokeComponent? GetAreaEffectComponent(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SmokeSolutionAreaEffectComponent>(entity);
        }
    }
}
