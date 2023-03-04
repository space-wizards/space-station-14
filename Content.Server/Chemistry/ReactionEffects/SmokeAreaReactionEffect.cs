using Content.Server.Chemistry.Components;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SmokeAreaReactionEffect : AreaReactionEffect
    {
        protected override SolutionAreaEffectComponent? GetAreaEffectComponent(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SmokeSolutionAreaEffectComponent>(entity);
        }

        public static void SpawnSmoke(string entityPrototype, EntityCoordinates coords, Solution? contents, int amount, float duration, float spreadDelay,
            float removeDelay, SoundSpecifier? sound = null, IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);
            var ent = entityManager.SpawnEntity(entityPrototype, coords.SnapToGrid());

            if (entityManager.TryGetComponent<SmokeSolutionAreaEffectComponent>(ent, out var areaEffectComponent))
            {
                if (contents != null)
                {
                    areaEffectComponent.TryAddSolution(contents);
                }
                areaEffectComponent.Start(amount, duration, spreadDelay, removeDelay);

                entityManager.EntitySysManager.GetEntitySystem<AudioSystem>()
                    .PlayPvs(sound, ent, AudioParams.Default.WithVariation(0.125f));
            }
            else
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + entityPrototype);
                entityManager.QueueDeleteEntity(ent);
                return;
            }
        }
    }
}
