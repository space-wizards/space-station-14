using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using System.Text.Json.Serialization;
using Content.Shared.EntityEffects;

namespace Content.Server.Corvax.GuideGenerator;
public sealed class ReagentEffectEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("description")]
    public string Description { get; }

    public ReagentEffectEntry(EntityEffect proto)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var entSys = IoCManager.Resolve<IEntitySystemManager>();

        Id = proto.GetType().Name;
        Description = GuidebookEffectDescriptionToWeb(proto.GuidebookEffectDescription(prototype, entSys) ?? "");
    }

    private string GuidebookEffectDescriptionToWeb(string guideBookText)
    {
        guideBookText = guideBookText.Replace("[", "<");
        guideBookText = guideBookText.Replace("]", ">");
        guideBookText = guideBookText.Replace("color", "span");

        while (guideBookText.IndexOf("<span=") != -1)
        {
            var first = guideBookText.IndexOf("<span=") + "<span=".Length - 1;
            var last = guideBookText.IndexOf(">", first);
            var replacementString = guideBookText.Substring(first, last - first);
            var color = replacementString.Substring(1);
            guideBookText = guideBookText.Replace(replacementString, string.Format(" style=\"color: {0};\"", color));
        }

        return guideBookText;
    }
}
