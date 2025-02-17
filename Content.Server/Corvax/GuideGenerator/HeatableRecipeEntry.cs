using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Robust.Server.GameObjects;

namespace Content.Server.GuideGenerator;

public sealed class HeatableRecipeEntry
{
    /// <summary>
    ///     Id of recipe
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    ///     Human-readable name of recipe.
    ///     Should automatically be localized by default
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    ///     Type of recipe
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; }

    /// <summary>
    ///     Temp, required for "input" thing to become "result" thing
    /// </summary>
    [JsonPropertyName("minTemp")]
    public float MinTemp { get; }

    /// <summary>
    ///     Item that will be transformed into something with enough temp
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; }

    /// <summary>
    ///     Result of a recipe.
    ///     If it is null then recipe does not exist or we could not get recipe info.
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; }


    public HeatableRecipeEntry(
        ConstructionGraphPrototype constructionProto, // to get data from construction prototype (minTemp, result)
        EntityPrototype entityPrototype // to get entity data (name, input entity id)
    )
    {
        var graphID = "";
        var startNode = constructionProto.Nodes[constructionProto.Start!];
        if (entityPrototype.Components.TryGetComponent("Construction", out var constructionCompRaw)) // does entity actually has Construction component?
        {
            foreach (var nodeEdgeRaw in startNode.Edges) // because we don't know what node contains heating step (in case if it is not constructionProto.Start) let's check every node and see if we will get anything
            {
                var nodeEdge = (ConstructionGraphEdge)nodeEdgeRaw;
                foreach (var nodeStepRaw in nodeEdge.Steps)
                {
                    if (nodeStepRaw.GetType().Equals(typeof(TemperatureConstructionGraphStep))) // TemperatureConstructionGraphStep is used only in steaks recipes, so for now we can afford it
                    {
                        var nodeStep = (TemperatureConstructionGraphStep)nodeStepRaw;
                        graphID = nodeEdge.Target; // required to check when we need to leave second loop; this is the best solution, because nodeEdge.Target is marked as required datafield and cannot be null
                        ServerEntityManager em = new();
                        MinTemp = nodeStep.MinTemperature.HasValue ? nodeStep.MinTemperature.Value : 0;
                        Result = nodeStep.MinTemperature.HasValue ? constructionProto.Nodes[nodeEdge.Target].Entity.GetId(null, null, new GraphNodeEntityArgs(em)) : null;
                        break;
                    }
                }
                if (graphID != "") break; // we're done! let's leave!
            }
            if (graphID == "") // we've failed to get anything :(
            {
                MinTemp = 0;
                Result = null;
            }
        }
        else // if entity does not have construction component then it cannot be constructed - (c) Jason Statham
        {
            MinTemp = 0;
            Result = null;
        }
        Input = entityPrototype.ID;
        Name = TextTools.TextTools.CapitalizeString(entityPrototype.Name);
        Id = entityPrototype.ID;
        Type = "heatableRecipes";
    }
}
