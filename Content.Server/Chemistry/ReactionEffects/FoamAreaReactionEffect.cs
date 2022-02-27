using Content.Server.Chemistry.Components;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class FoamAreaReactionEffect : AreaReactionEffect
    {
        protected override SolutionAreaEffectComponent? GetAreaEffectComponent(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<FoamSolutionAreaEffectComponent>(entity);
        }

        public static void SpawnFoam(string entityPrototype, EntityCoordinates coords, Solution? contents, int amount, float duration, float spreadDelay,
            float removeDelay, SoundSpecifier sound, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            var ent = entityManager.SpawnEntity(entityPrototype, coords.SnapToGrid());

            var areaEffectComponent = entityManager.GetComponentOrNull<FoamSolutionAreaEffectComponent>(ent);

            if (areaEffectComponent == null)
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + entityPrototype);
                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(ent);
                return;
            }

            if (contents != null)
                areaEffectComponent.TryAddSolution(contents);
            areaEffectComponent.Start(amount, duration, spreadDelay, removeDelay);

            SoundSystem.Play(Filter.Pvs(ent), sound.GetSound(), ent, AudioHelpers.WithVariation(0.125f));
        }
    }
}
