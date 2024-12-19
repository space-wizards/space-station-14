using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Xenoarchaeology.Artifact;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class XenoArtifactCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<XenoArtifactComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("printMatrix")]
    public string PrintMatrix([PipedArgument] EntityUid ent)
    {
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(ent);

        var nodeCount = comp.NodeVertices.Length;

        var sb = new StringBuilder("\n  |");
        for (var i = 0; i < nodeCount; i++)
        {
            sb.Append($" {i:D2}|");
        }

        AddHorizontalFiller(sb);

        for (var i = 0; i < nodeCount; i++)
        {
            sb.Append($"\n{i:D2}|");
            for (var j = 0; j < nodeCount; j++)
            {
                var value = comp.NodeAdjacencyMatrix[i][j]
                    ? "X"
                    : " ";
                sb.Append($" {value} |");
            }
            AddHorizontalFiller(sb);
        }

        return sb.ToString();

        void AddHorizontalFiller(StringBuilder builder)
        {
            builder.AppendLine();
            builder.Append("--+");
            for (var i = 0; i < nodeCount; i++)
            {
                builder.Append($"---+");
            }
        }
    }

    [CommandImplementation("totalResearch")]
    public int TotalResearch([PipedArgument] EntityUid ent)
    {
        var artiSys = EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(ent);

        var sum = 0;

        var nodes = artiSys.GetAllNodes((ent, comp));
        foreach (var node in nodes)
        {
            sum += node.Comp.ResearchValue;
        }

        return sum;
    }

    [ValidatePrototypeId<EntityPrototype>]
    public const string ArtifactPrototype = "BaseXenoArtifact";

    [CommandImplementation("averageResearch")]
    public float AverageResearch()
    {
        const int n = 100;
        var sum = 0;

        for (var i = 0; i < n; i++)
        {
            var ent = Spawn(ArtifactPrototype, MapCoordinates.Nullspace);
            sum += TotalResearch(ent);
            Del(ent);
        }

        return (float) sum / n;
    }

    [CommandImplementation("unlockAllNodes")]
    public void UnlockAllNodes([PipedArgument] EntityUid ent)
    {
        var artiSys = EntityManager.System<XenoArtifactSystem>();
        var comp = EntityManager.GetComponent<XenoArtifactComponent>(ent);

        var nodes = artiSys.GetAllNodes((ent, comp));
        foreach (var node in nodes)
        {
            artiSys.SetNodeUnlocked((node, node.Comp));
        }
    }
}
