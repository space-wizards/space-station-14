using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Makes the condition fail if any entities on a tile have (or not) a component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ComponentInTile : IGraphCondition
    {
        /// <summary>
        ///     If true, any entity on the tile must have the component.
        ///     If false, no entity on the tile must have the component.
        /// </summary>
        [DataField("hasEntity")]
        public bool HasEntity { get; private set; }

        [DataField("examineText")]
        public string? ExamineText { get; }

        [DataField("guideText")]
        public string? GuideText { get; }

        [DataField("guideIcon")]
        public SpriteSpecifier? GuideIcon { get; }

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
            var indices = transform.Coordinates.ToVector2i(entityManager, IoCManager.Resolve<IMapManager>());
            var entities = indices.GetEntitiesInTile(transform.GridID, LookupFlags.Approximate | LookupFlags.IncludeAnchored, IoCManager.Resolve<IEntityLookup>());

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
