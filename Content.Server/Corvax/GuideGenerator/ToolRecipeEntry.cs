using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Robust.Server.GameObjects;

namespace Content.Server.GuideGenerator;

public sealed class ToolRecipeEntry // because of https://github.com/space-wizards/space-station-14/pull/20624, some recipes can now be cooked using tools
// actually, the code is pretty similar with HeatableRecipeEntry. The only difference is that we need ToolConstructionGraphStep instead of TemperatureConstructionGraphStep
// comments are left untouched :)
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
    ///     Type of tool that is used to convert input into result
    /// </summary>
    [JsonPropertyName("tool")]
    public string? Tool { get; }

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


    public ToolRecipeEntry(
        ConstructionGraphPrototype constructionProto, // to get data from construction prototype (Tool, result)
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
                    if (nodeStepRaw.GetType().Equals(typeof(ToolConstructionGraphStep))) // ToolConstructionGraphStep is used only in steaks recipes, so for now we can afford it
                    {
                        var nodeStep = (ToolConstructionGraphStep)nodeStepRaw;
                        graphID = nodeEdge.Target; // required to check when we need to leave second loop; this is the best solution, because nodeEdge.Target is marked as required datafield and cannot be null
                        ServerEntityManager em = new();
                        Tool = nodeStep.Tool;
                        Result = constructionProto.Nodes[nodeEdge.Target].Entity.GetId(null, null, new GraphNodeEntityArgs(em));
                        break;
                    }
                }
                if (graphID != "") break; // we're done! let's leave!
            }
            if (graphID == "") // we've failed to get anything :(
            {
                Tool = null;
                Result = null;
            }
        }
        else // if entity does not have construction component then it cannot be constructed - (c) Jason Statham
        {
            Tool = null;
            Result = null;
        }
        Input = entityPrototype.ID;
        Name = TextTools.TextTools.CapitalizeString(entityPrototype.Name);
        Id = entityPrototype.ID;
        Type = "toolmadeRecipes";
    }
}
