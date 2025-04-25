using System.Linq;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Construction
{
    public abstract class SharedConstructionSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

        private Dictionary<string, string>? _recipesMetadataCache;

        public override void Initialize()
        {
            OnRecipeRequested(new(), new());
        }

        private void OnRecipeRequested(RequestConstructionRecipes msg, EntitySessionEventArgs args)
        {
            if (_recipesMetadataCache is null)
            {
                _recipesMetadataCache = new();
                foreach (var constructionProto in PrototypeManager.EnumeratePrototypes<ConstructionPrototype>())
                {
                    if (!PrototypeManager.TryIndex(constructionProto.Graph, out var graphProto))
                        continue;

                    if (constructionProto.TargetNode is not { } targetNodeId)
                        continue;

                    if (!graphProto.Nodes.TryGetValue(targetNodeId, out var targetNode))
                        continue;

                    // Recursion is for wimps.
                    var stack = new Stack<ConstructionGraphNode>();
                    stack.Push(targetNode);

                    do
                    {
                        var node = stack.Pop();

                        // I never realized if this uid affects anything...
                        EntityUid? userUid = args.SenderSession.State.ControlledEntity.HasValue
                            ? GetEntity(args.SenderSession.State.ControlledEntity.Value)
                            : null;

                        // We try to get the id of the target prototype, if it fails, we try going through the edges.
                        if (node.Entity.GetId(null, userUid, new(EntityManager)) is not { } entityId)
                        {
                            // If the stack is not empty, there is a high probability that the loop will go to infinity.
                            if (stack.Count == 0)
                                foreach (var edge in node.Edges)
                                    if (graphProto.Nodes.TryGetValue(edge.Target, out var graphNode))
                                        stack.Push(graphNode);
                            continue;
                        }

                        // If we got the id of the prototype, we exit the “recursion” by clearing the stack.
                        stack.Clear();

                        _recipesMetadataCache.Add(constructionProto.ID, entityId);
                    } while (stack.Count > 0);
                }
            }

            RaiseNetworkEvent(new ResponseConstructionRecipes(_recipesMetadataCache), args.SenderSession.Channel);
        }

        /// <summary>
        ///     Get predicate for construction obstruction checks.
        /// </summary>
        public Ignored? GetPredicate(bool canBuildInImpassable, MapCoordinates coords)
        {
            if (!canBuildInImpassable)
                return null;

            if (!_mapManager.TryFindGridAt(coords, out _, out var grid))
                return null;

            var ignored = grid.GetAnchoredEntities(coords).ToHashSet();
            return e => ignored.Contains(e);
        }

        public string GetExamineName(GenericPartInfo info)
        {
            if (info.ExamineName is not null)
                return Loc.GetString(info.ExamineName.Value);

            return PrototypeManager.Index(info.DefaultPrototype).Name;
        }
    }
}
