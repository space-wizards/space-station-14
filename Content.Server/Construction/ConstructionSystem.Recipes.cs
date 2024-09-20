using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem : SharedConstructionSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private Dictionary<string, RecipeMetadata>? _recipesMetadataCache;

    private void InitializeRecipes()
    {
        SubscribeNetworkEvent<RequestConstructionRecipes>(OnRecipeRequested);
    }

    private void OnRecipeRequested(RequestConstructionRecipes msg, EntitySessionEventArgs args)
    {
        if (_recipesMetadataCache is null)
        {
            _recipesMetadataCache = new();
            foreach (var constructionProto in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (!_prototypeManager.TryIndex(constructionProto.Graph, out var graphProto))
                    continue;

                if (constructionProto.TargetNode is not {} targetNodeId)
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

                    if (!_prototypeManager.TryIndex(entityId, out var entity))
                        continue;

                    RecipeMetadata meta = _prototypeManager.TryIndex(entityId, out var proto)
                            ? new(proto.Name, proto.Description)
                            : new(null, null);

                    _recipesMetadataCache.Add(constructionProto.ID, meta);
                } while (stack.Count > 0);
            }
        }

        RaiseNetworkEvent(new ResponseConstructionRecipes(_recipesMetadataCache), args.SenderSession.Channel);
    }
}
