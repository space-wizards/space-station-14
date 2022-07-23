using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Shared.Chemistry.Reaction;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class SolutionAreaEffectSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionAreaEffectComponent, ReactionAttemptEvent>(OnReactionAttempt);
        }

        public override void Update(float frameTime)
        {
            foreach (var inception in EntityManager.EntityQuery<SolutionAreaEffectInceptionComponent>().ToArray())
            {
                inception.InceptionUpdate(frameTime);
            }
        }

        private void OnReactionAttempt(EntityUid uid, SolutionAreaEffectComponent component, ReactionAttemptEvent args)
        {
            if (args.Solution.Name != SolutionAreaEffectComponent.SolutionName)
                return;

            // Prevent smoke/foam fork bombs (smoke creating more smoke).
            foreach (var effect in args.Reaction.Effects)
            {
                if (effect is AreaReactionEffect)
                {
                    args.Cancel();
                    return;
                }
            }
        }
    }
}
