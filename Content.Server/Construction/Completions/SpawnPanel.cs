using Content.Server.Power.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    /// <summary>
    /// Spawns an APC Panel, either opened or not.
    /// Probably TODO: Make an action that takes a prototype and an action as arguments, that spawns the prototype and then calls the action with the UID passed in its args.
    /// </summary>
    public sealed class SpawnPanel : IGraphAction
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var oldTransformComponent = entityManager.GetComponent<TransformComponent>(uid);
            var coordinates = oldTransformComponent.Coordinates;
            

            EntityUid newApc = entityManager.SpawnEntity("BaseAPC", coordinates);
            var apc = entityManager.GetComponent<ApcComponent>(newApc);
            var newTransformComponent = entityManager.GetComponent<TransformComponent>(newApc);
            newTransformComponent.WorldRotation = oldTransformComponent.WorldRotation;

            apc.IsApcOpen = Open;
        }
    }
}
