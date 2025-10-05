using System.Linq;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Construction
{
    public abstract partial class SharedConstructionSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
        [Dependency] protected readonly SharedPopupSystem Popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeGuided();
        }

        /// <summary>
        ///     Get predicate for construction obstruction checks.
        /// </summary>
        public Ignored? GetPredicate(bool canBuildInImpassable, MapCoordinates coords)
        {
            if (!canBuildInImpassable)
                return null;

            if (!_mapManager.TryFindGridAt(coords, out var gridUid, out var grid))
                return null;

            var ignored = _map.GetAnchoredEntities((gridUid, grid), coords).ToHashSet();
            return e => ignored.Contains(e);
        }

        public string GetExamineName(GenericPartInfo info)
        {
            if (info.ExamineName is not null)
                return Loc.GetString(info.ExamineName.Value);

            return PrototypeManager.Index(info.DefaultPrototype).Name;
        }

        /// <summary>
        ///     Performs a number of <see cref="IGraphAction"/>s for a given entity, with an optional user entity.
        /// </summary>
        /// <param name="uid">The entity to perform the actions on.</param>
        /// <param name="userUid">An optional user entity to pass into the actions.</param>
        /// <param name="actions">The actions to perform.</param>
        /// <remarks>This method checks whether the given target entity exists before performing any actions.
        ///          If the entity is deleted by an action, it will short-circuit and stop performing the rest of actions.</remarks>
        public void PerformActions(EntityUid uid, EntityUid? userUid, IEnumerable<IGraphAction> actions)
        {
            foreach (var action in actions)
            {
                // If an action deletes the entity, we stop performing the rest of actions.
                if (!Exists(uid))
                    break;

                action.PerformAction(uid, userUid, EntityManager);
            }
        }
    }
}
