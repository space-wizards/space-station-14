using Content.Shared.Medical.Digestion.Components;

namespace Content.Shared.Medical.Digestion.Systems;

public sealed partial class DigestionSystem
{
    private void UpdateCachedPrototypes(Entity<DigestionComponent> digester)
    {
        foreach (var digestionTypeId in digester.Comp.SupportedDigestionTypes)
        {
            var digestionType = _protoManager.Index(digestionTypeId);
            foreach (var digestionReactionId in digestionType.DigestionReactions)
            {
                var digestionReaction = _protoManager.Index(digestionReactionId);
                digester.Comp.CachedDigestionReactions.Add(digestionReaction.Data);
            }
        }
    }
}
