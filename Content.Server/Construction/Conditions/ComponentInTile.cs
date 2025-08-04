using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Makes the condition fail if any entities on a tile have (or not) a component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ComponentInTile : IGraphCondition
    {
        /// <summary>
        ///     If true, any entity on the tile must have the component.
        ///     If false, no entity on the tile must have the component.
        /// </summary>
        [DataField("hasEntity")]
        public bool HasEntity { get; private set; }

        [DataField("examineText")]
        public string? ExamineText { get; private set; }

        [DataField("guideText")]
        public string? GuideText { get; private set; }

        [DataField("guideIcon")]
        public SpriteSpecifier? GuideIcon { get; private set; }

        /// <summary>
        ///     The component name in question.
        /// </summary>
        [DataField("component")]
        public string Component { get; private set; } = string.Empty;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Component)) return false;

            var type = IoCManager.Resolve<IComponentFactory>().GetRegistration(Component).Type;

            var transform = entityManager.GetComponent<TransformComponent>(uid);
            if (transform.GridUid == null)
                return false;

            var transformSys = entityManager.System<SharedTransformSystem>();
            var indices = transform.Coordinates.ToVector2i(entityManager, IoCManager.Resolve<IMapManager>(), transformSys);
            var lookup = entityManager.EntitySysManager.GetEntitySystem<EntityLookupSystem>();


            if (!entityManager.TryGetComponent<MapGridComponent>(transform.GridUid.Value, out var grid))
                return !HasEntity;

            if (!entityManager.System<SharedMapSystem>().TryGetTileRef(transform.GridUid.Value, grid, indices, out var tile))
                return !HasEntity;

            var entities = tile.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.Static, lookup);

            foreach (var ent in entities)
            {
                if (entityManager.HasComponent(ent, type))
                    return HasEntity;
            }

            return !HasEntity;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            if (string.IsNullOrEmpty(ExamineText))
                return false;

            args.PushMarkup(Loc.GetString(ExamineText));
            return true;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            if (string.IsNullOrEmpty(GuideText))
                yield break;

            yield return new ConstructionGuideEntry()
            {
                Localization = GuideText,
                Icon = GuideIcon,
            };
        }
    }
}
